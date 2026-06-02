using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Structs;
using Reorbita.Api.Infrastructure;

namespace Reorbita.Api.Services;

public sealed partial class ServicoMonitoramento
{
    private IReadOnlyCollection<Alerta> ProcessarAnalisePreditiva(Satelite satelite, LeituraTelemetria leituraTelemetria)
    {
        try
        {
            var alertasGerados = _motorPreditivo.AnalisarTelemetria(satelite, leituraTelemetria).ToList();
            var previsaoFalha = _motorPreditivo.ProjetarFalha(satelite);

            if (previsaoFalha.GrauDeConfianca >= 0.35 &&
                previsaoFalha.DataHoraFalhaEstimada < DateTime.UtcNow.AddDays(30))
            {
                alertasGerados.Add(new AlertaPreventivo(
                    satelite.Id,
                    $"Falha projetada para {previsaoFalha.DataHoraFalhaEstimada:O}. Tipo provavel: {previsaoFalha.TipoProjetado}."));
            }

            _logger.LogInformation(
                "Previsao calculada. Satelite={SateliteId}, Tipo={TipoFalha}, Confianca={Confianca}, DataEstimada={DataHoraFalhaEstimada}",
                satelite.Id,
                previsaoFalha.TipoProjetado,
                previsaoFalha.GrauDeConfianca,
                previsaoFalha.DataHoraFalhaEstimada);

            foreach (var alerta in alertasGerados)
            {
                var resultadoProcessamento = _servicoAlerta.ProcessarAlerta(alerta);
                if (!resultadoProcessamento.Sucesso)
                {
                    _logger.LogWarning(
                        "Falha ao processar alerta. Satelite={SateliteId}, Motivo={Motivo}",
                        satelite.Id,
                        resultadoProcessamento.Mensagem);
                }
            }

            return alertasGerados.AsReadOnly();
        }
        catch (Exception exception)
        {
            return TratarFalhaAnalisePreditiva(exception, satelite.Id);
        }
    }

    private void AtualizarStatusPosAnalise(Satelite satelite, IReadOnlyCollection<Alerta> alertasGerados)
    {
        try
        {
            if (alertasGerados.Any(alerta => alerta.TipoAlerta is TipoAlerta.Critico))
            {
                satelite.ReceberAlerta(new AlertaCritico(satelite.Id, "Status atualizado para critico apos analise preditiva."));
                return;
            }

            if (alertasGerados.Any(alerta => alerta.TipoAlerta is TipoAlerta.Preventivo))
            {
                satelite.ReceberAlerta(new AlertaPreventivo(satelite.Id, "Status atualizado para degradado apos analise preditiva."));
                return;
            }

            satelite.AvaliarSaude();
        }
        catch (Exception exception)
        {
            TratarFalhaAtualizacaoStatus(exception, satelite.Id);
        }
    }

    private IReadOnlyCollection<Alerta> TratarFalhaAnalisePreditiva(Exception exception, string sateliteId)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, "Satelite nao encontrado durante analise preditiva no monitoramento. Satelite={SateliteId}", sateliteException.SateliteId);
                break;
            case TelemetriaInvalidaException telemetriaException:
                _logger.LogWarning(telemetriaException, "Telemetria invalida durante analise preditiva no monitoramento. Motivo={Motivo}", telemetriaException.Motivo);
                break;
            case IntervencaoNaoAutorizadaException intervencaoException:
                _logger.LogCritical(intervencaoException, "Intervencao nao autorizada durante analise preditiva no monitoramento. Satelite={SateliteId}, Robo={RoboId}", intervencaoException.SateliteId, intervencaoException.RoboId);
                AlertaSegurancaHelper.RegistrarEscaladaPrivilegio(_logger, intervencaoException.SateliteId, intervencaoException.RoboId);
                break;
            case RecursoOrbitalIndisponivelException recursoException:
                _logger.LogWarning(recursoException, "Recurso orbital indisponivel durante analise preditiva no monitoramento. Tipo={TipoSolicitado}", recursoException.TipoSolicitado);
                break;
            case FalhaDeComunicacaoOrbitalException falhaComunicacaoException:
                _logger.LogError(falhaComunicacaoException, "Falha de comunicacao orbital durante analise preditiva no monitoramento. Satelite={SateliteId}", sateliteId);
                break;
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, "Integridade comprometida durante analise preditiva no monitoramento. Arquivo={Arquivo}", integridadeException.CaminhoArquivo);
                break;
            default:
                _logger.LogError(exception, "Erro inesperado durante analise preditiva no monitoramento. Satelite={SateliteId}", sateliteId);
                break;
        }

        return [];
    }

    private void TratarFalhaAtualizacaoStatus(Exception exception, string sateliteId)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, "Satelite nao encontrado ao atualizar status pos-analise. Satelite={SateliteId}", sateliteException.SateliteId);
                break;
            case TelemetriaInvalidaException telemetriaException:
                _logger.LogWarning(telemetriaException, "Telemetria invalida ao atualizar status pos-analise. Satelite={SateliteId}, Motivo={Motivo}", sateliteId, telemetriaException.Motivo);
                break;
            case IntervencaoNaoAutorizadaException intervencaoException:
                _logger.LogCritical(intervencaoException, "Intervencao nao autorizada ao atualizar status pos-analise. Satelite={SateliteId}, Robo={RoboId}", intervencaoException.SateliteId, intervencaoException.RoboId);
                AlertaSegurancaHelper.RegistrarEscaladaPrivilegio(_logger, intervencaoException.SateliteId, intervencaoException.RoboId);
                break;
            case RecursoOrbitalIndisponivelException recursoException:
                _logger.LogWarning(recursoException, "Recurso orbital indisponivel ao atualizar status pos-analise. Tipo={TipoSolicitado}", recursoException.TipoSolicitado);
                break;
            case FalhaDeComunicacaoOrbitalException falhaComunicacaoException:
                _logger.LogError(falhaComunicacaoException, "Falha de comunicacao orbital ao atualizar status pos-analise. Satelite={SateliteId}", sateliteId);
                break;
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, "Integridade comprometida ao atualizar status pos-analise. Arquivo={Arquivo}", integridadeException.CaminhoArquivo);
                break;
            default:
                _logger.LogError(exception, "Erro inesperado ao atualizar status pos-analise. Satelite={SateliteId}", sateliteId);
                break;
        }
    }
}
