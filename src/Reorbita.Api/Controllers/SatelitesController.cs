using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;
using Reorbita.Api.Models.Requests;
using Reorbita.Api.Models.Responses;

namespace Reorbita.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/satelites")]
public sealed class SatelitesController : ControllerBase
{
    private readonly IServicoMonitoramento _servicoMonitoramento;

    public SatelitesController(IServicoMonitoramento servicoMonitoramento)
    {
        _servicoMonitoramento = servicoMonitoramento;
    }

    [HttpGet]
    public IActionResult ListarSatelitesDaOperadora()
    {
        var operadora = ObterOperadoraAutenticada();
        if (string.IsNullOrWhiteSpace(operadora))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = "Claim de operadora nao encontrada no token.",
                CodigoErro = "OPERADORA_NAO_IDENTIFICADA"
            });
        }

        var resultado = _servicoMonitoramento.ListarSatelitesPorOperadora(operadora);
        return CriarResposta(
            resultado,
            satelites => satelites.Select(satelite => satelite.ParaResponse()).ToList().AsReadOnly());
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(string id)
    {
        var operadora = ObterOperadoraAutenticada();
        if (string.IsNullOrWhiteSpace(operadora))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = "Claim de operadora nao encontrada no token.",
                CodigoErro = "OPERADORA_NAO_IDENTIFICADA"
            });
        }

        var resultado = _servicoMonitoramento.ObterSatelitePorId(id, operadora);
        return CriarResposta(resultado, satelite => satelite.ParaResponse());
    }

    [HttpPost]
    [Authorize(Roles = "OperadoraEscrita,OperadoraAdmin,ReorbitaAdmin")]
    public IActionResult CadastrarSatelite([FromBody] CriarSateliteRequest request)
    {
        var satelite = request.ParaEntidade();
        var resultado = _servicoMonitoramento.CadastrarSatelite(satelite);
        return CriarResposta(resultado, entidade => entidade.ParaResponse());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "OperadoraEscrita,OperadoraAdmin,ReorbitaAdmin")]
    public IActionResult AtualizarSatelite(string id, [FromBody] AtualizarSateliteRequest request)
    {
        var operadora = ObterOperadoraAutenticada();
        if (string.IsNullOrWhiteSpace(operadora))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = "Claim de operadora nao encontrada no token.",
                CodigoErro = "OPERADORA_NAO_IDENTIFICADA"
            });
        }

        var resultadoBusca = _servicoMonitoramento.ObterSatelitePorId(id, operadora);
        if (!resultadoBusca.Sucesso || resultadoBusca.Dados is null)
        {
            return StatusCode(resultadoBusca.CodigoHttp, new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = resultadoBusca.Mensagem,
                CodigoErro = resultadoBusca.CodigoErro
            });
        }

        var entidadeAtualizada = request.ParaEntidadeAtualizada(resultadoBusca.Dados);
        var resultadoAtualizacao = _servicoMonitoramento.AtualizarSatelite(id, entidadeAtualizada, operadora);

        return CriarResposta(resultadoAtualizacao, satelite => satelite.ParaResponse());
    }

    private string ObterOperadoraAutenticada()
    {
        return User.FindFirstValue("operadora") ?? string.Empty;
    }

    private IActionResult CriarResposta<TDominio, TApi>(ResultadoOperacao<TDominio> resultadoOperacao, Func<TDominio, TApi> mapear)
    {
        if (!resultadoOperacao.Sucesso)
        {
            return StatusCode(resultadoOperacao.CodigoHttp, new ApiResponse<TApi>
            {
                Sucesso = false,
                Mensagem = resultadoOperacao.Mensagem,
                CodigoErro = resultadoOperacao.CodigoErro
            });
        }

        return StatusCode(resultadoOperacao.CodigoHttp, new ApiResponse<TApi>
        {
            Sucesso = true,
            Mensagem = resultadoOperacao.Mensagem,
            Dados = mapear(resultadoOperacao.Dados!)
        });
    }
}
