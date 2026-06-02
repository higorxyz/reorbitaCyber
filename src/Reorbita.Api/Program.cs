using System.Security.Claims;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Infrastructure;
using Reorbita.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls13;
    });
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var usarPersistenciaArquivo = (builder.Configuration["Persistencia:Modo"] ?? "Memoria")
    .Equals("Arquivo", StringComparison.OrdinalIgnoreCase);

// Repositorios registrados por escopo de requisicao.
builder.Services.AddScoped<IRepositorioSatelite>(serviceProvider =>
{
    return usarPersistenciaArquivo
        ? ActivatorUtilities.CreateInstance<RepositorioSateliteArquivo>(serviceProvider)
        : ActivatorUtilities.CreateInstance<RepositorioSateliteMemoria>(serviceProvider);
});

builder.Services.AddScoped<IRepositorioFrota>(serviceProvider =>
{
    return usarPersistenciaArquivo
        ? ActivatorUtilities.CreateInstance<RepositorioFrotaArquivo>(serviceProvider)
        : ActivatorUtilities.CreateInstance<RepositorioFrotaMemoria>(serviceProvider);
});

// Servicos de negocio registrados por escopo de requisicao.
builder.Services.AddScoped<IMotorPreditivo, MotorPreditivoReorbita>();
builder.Services.AddScoped<IServicoAlerta, ServicoAlerta>();
builder.Services.AddScoped<ServicoFrota>();
builder.Services.AddScoped<IServicoFrota>(serviceProvider => serviceProvider.GetRequiredService<ServicoFrota>());
builder.Services.AddScoped<ServicoMonitoramento>();
builder.Services.AddScoped<IServicoMonitoramento>(serviceProvider => serviceProvider.GetRequiredService<ServicoMonitoramento>());

builder.Services.AddScoped<IServicoAutenticacao, ServicoAutenticacao>();

builder.Services.AddSingleton<IHashCredencial, BcryptHashCredencial>();
builder.Services.AddSingleton<TokenRevogacaoStore>();

var chaveJwt = new SymmetricSecurityKey(JwtChaveProvider.ObterChave());
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Seguranca:JwtIssuer"],
            ValidAudience = builder.Configuration["Seguranca:JwtAudience"],
            IssuerSigningKey = chaveJwt,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenRevogacaoStore = context.HttpContext.RequestServices.GetRequiredService<TokenRevogacaoStore>();
                var tokenId = context.Principal?.FindFirstValue("jti");

                if (!string.IsNullOrWhiteSpace(tokenId) && tokenRevogacaoStore.EstaRevogado(tokenId))
                {
                    context.Fail("Token revogado.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FrotaComando", policy =>
        policy.RequireAssertion(context =>
        {
            var papeis = context.User
                .FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (papeis.Contains(NivelAcesso.OperadoraAdmin.ToString()))
            {
                return true;
            }

            if (!papeis.Contains(NivelAcesso.ReorbitaAdmin.ToString()))
            {
                return false;
            }

            var mfa = context.User.FindFirstValue("mfa");
            return string.Equals(mfa, "true", StringComparison.OrdinalIgnoreCase);
        }));

    options.AddPolicy("AdminComMfa", policy =>
        policy.RequireRole(NivelAcesso.ReorbitaAdmin.ToString())
              .RequireClaim("mfa", "true"));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("telemetria-ingestao", config =>
    {
        config.PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:TelemetriaPermitLimit", 30);
        config.Window = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("RateLimiting:TelemetriaJanelaSegundos", 60));
        config.QueueLimit = 0;
    });
});

// Exposicao da documentacao OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "REORBITA API",
        Version = "v1",
        Description = "Plataforma de Inteligencia Orbital para monitoramento, alertas e intervencoes de frota robotica."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe o token JWT no formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Logging em JSON para observabilidade.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Tratamento global de excecoes no inicio do pipeline.
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseMiddleware<ValidacaoMtlsCanalRobosMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
