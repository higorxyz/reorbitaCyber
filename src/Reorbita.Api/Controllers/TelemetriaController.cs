using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;
using Reorbita.Api.Models.Requests;
using Reorbita.Api.Models.Responses;

namespace Reorbita.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/telemetria")]
public sealed class TelemetriaController : ControllerBase
{
    private readonly IServicoMonitoramento _servicoMonitoramento;

    public TelemetriaController(IServicoMonitoramento servicoMonitoramento)
    {
        _servicoMonitoramento = servicoMonitoramento;
    }

    [HttpPost("{sateliteId}")]
    [EnableRateLimiting("telemetria-ingestao")]
    public IActionResult ReceberTelemetria(string sateliteId, [FromBody] ReceberTelemetriaRequest request)
    {
        var leituraTelemetria = new LeituraTelemetria(
            request.SensorId,
            request.Valor,
            request.Unidade,
            request.DataHoraColetaUtc ?? DateTime.UtcNow,
            DentroDoLimiteNormal: true);

        var resultado = _servicoMonitoramento.ReceberTelemetria(sateliteId, leituraTelemetria);

        if (!resultado.Sucesso)
        {
            return StatusCode(resultado.CodigoHttp, new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = resultado.Mensagem,
                CodigoErro = resultado.CodigoErro
            });
        }

        return StatusCode(resultado.CodigoHttp, new ApiResponse<RelatorioSaudeResponse>
        {
            Sucesso = true,
            Mensagem = resultado.Mensagem,
            Dados = resultado.Dados!.ParaResponse()
        });
    }

    [HttpGet("{sateliteId}/historico")]
    public IActionResult ConsultarHistorico(
        string sateliteId,
        [FromQuery] DateTime? dataInicioUtc,
        [FromQuery] DateTime? dataFimUtc)
    {
        var resultado = _servicoMonitoramento.ObterHistoricoTelemetria(sateliteId, dataInicioUtc, dataFimUtc);
        if (!resultado.Sucesso)
        {
            return StatusCode(resultado.CodigoHttp, new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = resultado.Mensagem,
                CodigoErro = resultado.CodigoErro
            });
        }

        return StatusCode(resultado.CodigoHttp, new ApiResponse<IReadOnlyCollection<LeituraTelemetria>>
        {
            Sucesso = true,
            Mensagem = resultado.Mensagem,
            Dados = resultado.Dados
        });
    }
}
