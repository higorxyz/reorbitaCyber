using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;
using Reorbita.Api.Infrastructure;

namespace Reorbita.Api.Services;

public sealed class MotorPreditivoReorbita : IMotorPreditivo
{
    private const double LIMIAR_DESVIO_ORBITAL = 1100.0;
    private readonly ILogger<MotorPreditivoReorbita> _logger;

    public MotorPreditivoReorbita(ILogger<MotorPreditivoReorbita> logger)
    {
        _logger = logger;
    }

    public IReadOnlyCollection<Alerta> AnalisarTelemetria(Satelite satelite, LeituraTelemetria leituraTelemetria)
    {
        try
        {
            var alertasGerados = new List<Alerta>();
            var sensorNormalizado = leituraTelemetria.SensorId.Trim().ToLowerInvariant();

            if (sensorNormalizado.Contains("bateria"))
            {
                if (leituraTelemetria.Valor < 10.0)
                {
                    alertasGerados.Add(new AlertaCritico(satelite.Id, "Bateria abaixo de 10%. Risco imediato de inoperancia."));
                }
                else
                {
                    var historicoBateria = satelite
                        .ObterLeiturasUltimasHoras(72)
                        .Where(leitura => leitura.SensorId.Contains("bateria", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(leitura => leitura.DataHoraColeta)
                        .ToList();

                    if (historicoBateria.Count >= 2)
                    {
                        var primeiroValor = historicoBateria.First().Valor;
                        var ultimoValor = historicoBateria.Last().Valor;
                        var horasDecorridas = Math.Max(1.0, (historicoBateria.Last().DataHoraColeta - historicoBateria.First().DataHoraColeta).TotalHours);
                        var quedaPorHora = (primeiroValor - ultimoValor) / horasDecorridas;
                        var projecaoEm30Dias = leituraTelemetria.Valor - (quedaPorHora * 24.0 * 30.0);

                        if (projecaoEm30Dias < 20.0)
                        {
                            alertasGerados.Add(new AlertaPreventivo(satelite.Id, "Tendencia de bateria abaixo de 20% em 30 dias."));
                        }
                    }
                }
            }

            if (sensorNormalizado.Contains("propulsao"))
            {
                var leiturasPropulsao = satelite
                    .ObterLeiturasUltimasHoras(24)
                    .Where(leitura => leitura.SensorId.Contains("propulsao", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (leiturasPropulsao.Count > 0)
                {
                    var leiturasAnomalas = leiturasPropulsao.Count(leitura => !leitura.DentroDoLimiteNormal);
                    var percentualAnomalia = (double)leiturasAnomalas / leiturasPropulsao.Count;
                    if (percentualAnomalia > 0.30)
                    {
                        alertasGerados.Add(new AlertaPreventivo(satelite.Id, "Propulsao com anomalias acima de 30% nas ultimas 24h."));
                    }
                }
            }

            if (sensorNormalizado.Contains("orbitacao") && leituraTelemetria.Valor > LIMIAR_DESVIO_ORBITAL)
            {
                alertasGerados.Add(new AlertaCritico(satelite.Id, "Desvio orbital acima do limiar configurado."));
            }

            if (sensorNormalizado.Contains("painel"))
            {
                var leiturasPainel = satelite
                    .ObterHistoricoTelemetria(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
                    .Where(leitura => leitura.SensorId.Contains("painel", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(leitura => leitura.DataHoraColeta)
                    .ToList();

                if (leiturasPainel.Count >= 2)
                {
                    var variacao = leiturasPainel.Last().Valor - leiturasPainel.First().Valor;
                    if (variacao > 15.0)
                    {
                        alertasGerados.Add(new AlertaPreventivo(satelite.Id, "Degradacao de painel solar acima de 15% em 7 dias."));
                    }
                }
            }

            _logger.LogInformation("Motor preditivo analisou alertas. TotalAlertas={TotalAlertas}, Satelite={SateliteId}", alertasGerados.Count, satelite.Id);
            return alertasGerados;
        }
        catch (Exception exception)
        {
            return TratarFalhaAnalisePreditiva(exception, satelite.Id);
        }
    }

    public PrevisaoFalha ProjetarFalha(Satelite satelite)
    {
        try
        {
            var leiturasPeriodo = satelite.ObterHistoricoTelemetria(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
            if (leiturasPeriodo.Count == 0)
            {
                return new PrevisaoFalha(satelite.Id, TipoFalha.FalhaComunicacao, DateTime.UtcNow.AddDays(45), 0.10);
            }

            var totalLeituras = leiturasPeriodo.Count;
            var leiturasAnomalas = leiturasPeriodo.Count(leitura => !leitura.DentroDoLimiteNormal);
            var grauConfianca = Math.Clamp((double)leiturasAnomalas / totalLeituras, 0.0, 1.0);

            var tipoProjetado = DeterminarTipoFalhaProvavel(leiturasPeriodo);
            var diasAteFalha = Math.Clamp(30.0 * (1.0 - grauConfianca), 2.0, 30.0);

            // TODO: calibrar o modelo com base historica maior.
            return new PrevisaoFalha(
                satelite.Id,
                tipoProjetado,
                DateTime.UtcNow.AddDays(diasAteFalha),
                grauConfianca);
        }
        catch (Exception exception)
        {
            return TratarFalhaProjecao(exception, satelite.Id);
        }
    }

    private IReadOnlyCollection<Alerta> TratarFalhaAnalisePreditiva(Exception exception, string sateliteId)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, "Satelite nao encontrado na analise preditiva. Satelite={SateliteId}", sateliteException.SateliteId);
                break;
            case TelemetriaInvalidaException telemetriaException:
                _logger.LogWarning(telemetriaException, "Telemetria invalida na analise preditiva. Motivo={Motivo}", telemetriaException.Motivo);
                break;
            case IntervencaoNaoAutorizadaException intervencaoException:
                _logger.LogCritical(intervencaoException, "Intervencao nao autorizada durante analise preditiva. Satelite={SateliteId}, Robo={RoboId}", intervencaoException.SateliteId, intervencaoException.RoboId);
                AlertaSegurancaHelper.RegistrarEscaladaPrivilegio(_logger, intervencaoException.SateliteId, intervencaoException.RoboId);
                break;
            case RecursoOrbitalIndisponivelException recursoException:
                _logger.LogWarning(recursoException, "Recurso orbital indisponivel durante analise preditiva. Tipo={TipoSolicitado}", recursoException.TipoSolicitado);
                break;
            case FalhaDeComunicacaoOrbitalException falhaComunicacaoException:
                _logger.LogError(falhaComunicacaoException, "Falha de comunicacao orbital durante analise preditiva. Satelite={SateliteId}", sateliteId);
                break;
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, "Integridade comprometida durante analise preditiva. Arquivo={Arquivo}", integridadeException.CaminhoArquivo);
                break;
            default:
                _logger.LogError(exception, "Erro inesperado durante analise preditiva. Satelite={SateliteId}", sateliteId);
                break;
        }

        return [];
    }

    private PrevisaoFalha TratarFalhaProjecao(Exception exception, string sateliteId)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, "Satelite nao encontrado na projecao de falha. Satelite={SateliteId}", sateliteException.SateliteId);
                return CriarPrevisaoFallback("DESCONHECIDO");
            case TelemetriaInvalidaException telemetriaException:
                _logger.LogWarning(telemetriaException, "Telemetria invalida na projecao de falha. Motivo={Motivo}", telemetriaException.Motivo);
                return CriarPrevisaoFallback(sateliteId);
            case IntervencaoNaoAutorizadaException intervencaoException:
                _logger.LogCritical(intervencaoException, "Intervencao nao autorizada durante projecao de falha. Satelite={SateliteId}, Robo={RoboId}", intervencaoException.SateliteId, intervencaoException.RoboId);
                AlertaSegurancaHelper.RegistrarEscaladaPrivilegio(_logger, intervencaoException.SateliteId, intervencaoException.RoboId);
                return CriarPrevisaoFallback(sateliteId);
            case RecursoOrbitalIndisponivelException recursoException:
                _logger.LogWarning(recursoException, "Recurso orbital indisponivel durante projecao de falha. Tipo={TipoSolicitado}", recursoException.TipoSolicitado);
                return CriarPrevisaoFallback(sateliteId);
            case FalhaDeComunicacaoOrbitalException falhaComunicacaoException:
                _logger.LogError(falhaComunicacaoException, "Falha de comunicacao orbital durante projecao de falha. Satelite={SateliteId}", sateliteId);
                return CriarPrevisaoFallback(sateliteId);
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, "Integridade comprometida durante projecao de falha. Arquivo={Arquivo}", integridadeException.CaminhoArquivo);
                return CriarPrevisaoFallback(sateliteId);
            default:
                _logger.LogError(exception, "Erro inesperado durante projecao de falha. Satelite={SateliteId}", sateliteId);
                return CriarPrevisaoFallback(sateliteId);
        }
    }

    private static PrevisaoFalha CriarPrevisaoFallback(string sateliteId)
    {
        return new PrevisaoFalha(sateliteId, TipoFalha.FalhaComunicacao, DateTime.UtcNow.AddDays(30), 0.0);
    }

    private static TipoFalha DeterminarTipoFalhaProvavel(IReadOnlyCollection<LeituraTelemetria> leituras)
    {
        var contagemPorTipo = new Dictionary<TipoFalha, int>
        {
            [TipoFalha.FalhaBateria] = leituras.Count(leitura => leitura.SensorId.Contains("bateria", StringComparison.OrdinalIgnoreCase) && !leitura.DentroDoLimiteNormal),
            [TipoFalha.FalhaPropulsao] = leituras.Count(leitura => leitura.SensorId.Contains("propulsao", StringComparison.OrdinalIgnoreCase) && !leitura.DentroDoLimiteNormal),
            [TipoFalha.FalhaPainelSolar] = leituras.Count(leitura => leitura.SensorId.Contains("painel", StringComparison.OrdinalIgnoreCase) && !leitura.DentroDoLimiteNormal),
            [TipoFalha.DesvioOrbital] = leituras.Count(leitura => leitura.SensorId.Contains("orbitacao", StringComparison.OrdinalIgnoreCase) && !leitura.DentroDoLimiteNormal),
            [TipoFalha.FalhaComunicacao] = leituras.Count(leitura => leitura.SensorId.Contains("comunicacao", StringComparison.OrdinalIgnoreCase) && !leitura.DentroDoLimiteNormal)
        };

        return contagemPorTipo
            .OrderByDescending(par => par.Value)
            .ThenBy(par => par.Key)
            .First().Key;
    }
}
