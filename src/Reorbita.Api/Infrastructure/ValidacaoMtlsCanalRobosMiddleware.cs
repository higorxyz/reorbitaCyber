using System.Text.Json;
using Reorbita.Api.Models.Responses;

namespace Reorbita.Api.Infrastructure;

public sealed class ValidacaoMtlsCanalRobosMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ValidacaoMtlsCanalRobosMiddleware> _logger;

    public ValidacaoMtlsCanalRobosMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ValidacaoMtlsCanalRobosMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var exigirMtls = _configuration.GetValue<bool>("Seguranca:ExigirMtlsCanalRobos", false);
        if (!exigirMtls)
        {
            await _next(context);
            return;
        }

        var endpointComandoFrota = context.Request.Path.StartsWithSegments("/api/frota/intervencao", StringComparison.OrdinalIgnoreCase);
        if (!endpointComandoFrota)
        {
            await _next(context);
            return;
        }

        var certificadoCliente = context.Connection.ClientCertificate;
        var thumbprintEncaminhado = context.Request.Headers["X-Client-Cert-Thumbprint"].ToString();

        var canalSeguro = certificadoCliente is not null || !string.IsNullOrWhiteSpace(thumbprintEncaminhado);
        if (canalSeguro)
        {
            await _next(context);
            return;
        }

        _logger.LogWarning("Requisicao de comando da frota bloqueada por ausencia de mTLS.");

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var respostaErro = new ApiResponse<object>
        {
            Sucesso = false,
            Mensagem = "Canal mTLS obrigatorio para comandos da frota orbital.",
            CodigoErro = "CANAL_MTLS_OBRIGATORIO"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(respostaErro));
    }
}
