using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Models.Requests;
using Reorbita.Api.Models.Responses;

namespace Reorbita.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/frota")]
public sealed class FrotaController : ControllerBase
{
    private readonly IServicoFrota _servicoFrota;

    public FrotaController(IServicoFrota servicoFrota)
    {
        _servicoFrota = servicoFrota;
    }

    [HttpPost("intervencao")]
    [Authorize(Policy = "FrotaComando")]
    public IActionResult SolicitarIntervencao([FromBody] SolicitarIntervencaoRequest request)
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

        var nivelAcesso = ObterNivelAcesso(User);
        var resultado = _servicoFrota.SolicitarIntervencao(request.SateliteId, request.TipoIntervencao, operadora, nivelAcesso);

        if (!resultado.Sucesso)
        {
            return StatusCode(resultado.CodigoHttp, new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = resultado.Mensagem,
                CodigoErro = resultado.CodigoErro
            });
        }

        return StatusCode(resultado.CodigoHttp, new ApiResponse<OrdemServicoResponse>
        {
            Sucesso = true,
            Mensagem = resultado.Mensagem,
            Dados = resultado.Dados!.ParaResponse()
        });
    }

    [HttpGet("ordens")]
    [Authorize(Roles = "OperadoraLeitura,OperadoraEscrita,OperadoraAdmin,ReorbitaAdmin")]
    public IActionResult ListarOrdensServico()
    {
        var resultado = _servicoFrota.ListarOrdens();
        if (!resultado.Sucesso)
        {
            return StatusCode(resultado.CodigoHttp, new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = resultado.Mensagem,
                CodigoErro = resultado.CodigoErro
            });
        }

        return StatusCode(resultado.CodigoHttp, new ApiResponse<IReadOnlyCollection<OrdemServicoResponse>>
        {
            Sucesso = true,
            Mensagem = resultado.Mensagem,
            Dados = resultado.Dados!.Select(ordem => ordem.ParaResponse()).ToList().AsReadOnly()
        });
    }

    [HttpGet("robos")]
    [Authorize(Roles = "OperadoraLeitura,OperadoraEscrita,OperadoraAdmin,ReorbitaAdmin")]
    public IActionResult ListarDisponibilidadeFrota()
    {
        var resultado = _servicoFrota.ListarRobos();
        if (!resultado.Sucesso)
        {
            return StatusCode(resultado.CodigoHttp, new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = resultado.Mensagem,
                CodigoErro = resultado.CodigoErro
            });
        }

        return StatusCode(resultado.CodigoHttp, new ApiResponse<IReadOnlyCollection<RoboOrbitalResponse>>
        {
            Sucesso = true,
            Mensagem = resultado.Mensagem,
            Dados = resultado.Dados!.Select(robo => robo.ParaResponse()).ToList().AsReadOnly()
        });
    }

    private static NivelAcesso ObterNivelAcesso(ClaimsPrincipal usuario)
    {
        var papel = usuario.FindFirstValue(ClaimTypes.Role)
            ?? usuario.FindFirstValue("role")
            ?? NivelAcesso.OperadoraLeitura.ToString();

        return Enum.TryParse<NivelAcesso>(papel, true, out var nivelAcesso)
            ? nivelAcesso
            : NivelAcesso.OperadoraLeitura;
    }
}
