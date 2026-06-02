using System.Text.Json;
using Reorbita.Api.Models.Responses;

namespace Reorbita.Api.Infrastructure;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Excecao nao tratada capturada pelo middleware global.");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = "Erro interno inesperado no processamento da requisicao.",
                CodigoErro = "ERRO_INTERNO_GLOBAL"
            };

            var json = JsonSerializer.Serialize(payload);
            await context.Response.WriteAsync(json);
        }
    }
}
