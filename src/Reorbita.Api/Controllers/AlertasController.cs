using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Models.Responses;

namespace Reorbita.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alertas")]
public sealed class AlertasController : ControllerBase
{
    private readonly IServicoAlerta _servicoAlerta;

    public AlertasController(IServicoAlerta servicoAlerta)
    {
        _servicoAlerta = servicoAlerta;
    }

    [HttpGet]
    public IActionResult ListarAlertasDaOperadora()
    {
        var operadora = User.FindFirstValue("operadora") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(operadora))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = "Claim de operadora nao encontrada no token.",
                CodigoErro = "OPERADORA_NAO_IDENTIFICADA"
            });
        }

        var resultado = _servicoAlerta.ListarAlertasPorOperadora(operadora);
        if (!resultado.Sucesso)
        {
            return StatusCode(resultado.CodigoHttp, new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = resultado.Mensagem,
                CodigoErro = resultado.CodigoErro
            });
        }

        return StatusCode(resultado.CodigoHttp, new ApiResponse<IReadOnlyCollection<AlertaResponse>>
        {
            Sucesso = true,
            Mensagem = resultado.Mensagem,
            Dados = resultado.Dados!.Select(alerta => alerta.ParaResponse()).ToList().AsReadOnly()
        });
    }

    [HttpGet("{sateliteId}")]
    public IActionResult ListarAlertasPorSatelite(string sateliteId)
    {
        var operadora = User.FindFirstValue("operadora") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(operadora))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = "Claim de operadora nao encontrada no token.",
                CodigoErro = "OPERADORA_NAO_IDENTIFICADA"
            });
        }

        var resultado = _servicoAlerta.ListarAlertasPorSatelite(sateliteId, operadora);
        if (!resultado.Sucesso)
        {
            return StatusCode(resultado.CodigoHttp, new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = resultado.Mensagem,
                CodigoErro = resultado.CodigoErro
            });
        }

        return StatusCode(resultado.CodigoHttp, new ApiResponse<IReadOnlyCollection<AlertaResponse>>
        {
            Sucesso = true,
            Mensagem = resultado.Mensagem,
            Dados = resultado.Dados!.Select(alerta => alerta.ParaResponse()).ToList().AsReadOnly()
        });
    }
}
