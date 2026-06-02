using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;
using Reorbita.Api.Infrastructure;

namespace Reorbita.Api.Services;

public sealed class ServicoAlerta : IServicoAlerta
{
    private readonly IRepositorioSatelite _repositorioSatelite;
    private readonly IServicoFrota _servicoFrota;
    private readonly ILogger<ServicoAlerta> _logger;
    private readonly List<Alerta> _alertasRegistrados = [];

    public ServicoAlerta(
        IRepositorioSatelite repositorioSatelite,
        IServicoFrota servicoFrota,
        ILogger<ServicoAlerta> logger)
    {
        _repositorioSatelite = repositorioSatelite;
        _servicoFrota = servicoFrota;
        _logger = logger;
    }

    public ResultadoOperacao<Alerta> ProcessarAlerta(Alerta alerta)
    {
        try
        {
            var satelite = _repositorioSatelite.ObterPorId(alerta.SateliteId)
                ?? throw new SateliteNaoEncontradoException(alerta.SateliteId);

            RegistrarNoLog(alerta);
            NotificarOperadora(satelite.Operadora, alerta.GerarDescricao());

            _alertasRegistrados.Add(alerta);

            if (alerta is AlertaCritico)
            {
                var resultadoFrota = _servicoFrota.SolicitarIntervencao(
                    satelite.Id,
                    TipoIntervencao.Reabastecimento,
                    satelite.Operadora,
                    NivelAcesso.ReorbitaAdmin);

                if (!resultadoFrota.Sucesso)
                {
                    _logger.LogWarning(
                        "Alerta critico sem intervencao automatica. Satelite={SateliteId}, Motivo={Motivo}",
                        satelite.Id,
                        resultadoFrota.Mensagem);
                }
            }

            return ResultadoOperacao<Alerta>.Ok(alerta, "Alerta processado com sucesso.", 201);
        }
        catch (Exception exception)
        {
            return TratarFalhaProcessamentoAlerta(exception, alerta.SateliteId);
        }
    }

    public void NotificarOperadora(string operadora, string mensagem)
    {
        try
        {
            // TODO: integrar notificacoes com webhook ou fila em ambiente produtivo.
            _logger.LogInformation("Notificacao enviada. Operadora={Operadora}, Mensagem={Mensagem}", operadora, mensagem);
        }
        catch (Exception exception)
        {
            TratarFalhaNotificacao(exception, operadora);
        }
    }

    public void RegistrarNoLog(Alerta alerta)
    {
        try
        {
            _logger.LogInformation(
                "Alerta registrado: Satelite={SateliteId}, Tipo={TipoAlerta}, DataHora={DataHoraCriacao}, Descricao={Descricao}",
                alerta.SateliteId,
                alerta.TipoAlerta,
                alerta.DataHoraCriacao,
                alerta.Descricao);
        }
        catch (Exception exception)
        {
            TratarFalhaRegistroLog(exception, alerta.SateliteId);
        }
    }

    private ResultadoOperacao<Alerta> TratarFalhaProcessamentoAlerta(Exception exception, string sateliteId)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, "Nao foi possivel processar alerta para satelite inexistente. Satelite={SateliteId}", sateliteException.SateliteId);
                return ResultadoOperacao<Alerta>.Falha(sateliteException.Message, 404, "SATELITE_NAO_ENCONTRADO");
            case TelemetriaInvalidaException telemetriaException:
                _logger.LogWarning(telemetriaException, "Alerta baseado em telemetria invalida. Motivo={Motivo}", telemetriaException.Motivo);
                return ResultadoOperacao<Alerta>.Falha(telemetriaException.Message, 400, "TELEMETRIA_INVALIDA");
            case IntervencaoNaoAutorizadaException intervencaoException:
                _logger.LogCritical(intervencaoException, "Intervencao nao autorizada durante processamento de alerta. Satelite={SateliteId}, Robo={RoboId}", intervencaoException.SateliteId, intervencaoException.RoboId);
                AlertaSegurancaHelper.RegistrarEscaladaPrivilegio(_logger, intervencaoException.SateliteId, intervencaoException.RoboId);
                return ResultadoOperacao<Alerta>.Falha(intervencaoException.Message, 403, "INTERVENCAO_NAO_AUTORIZADA");
            case RecursoOrbitalIndisponivelException recursoException:
                _logger.LogWarning(recursoException, "Recurso orbital indisponivel durante processamento de alerta. Tipo={TipoSolicitado}", recursoException.TipoSolicitado);
                return ResultadoOperacao<Alerta>.Falha(recursoException.Message, 409, "RECURSO_ORBITAL_INDISPONIVEL");
            case FalhaDeComunicacaoOrbitalException falhaComunicacaoException:
                _logger.LogError(falhaComunicacaoException, "Falha de comunicacao orbital ao processar alerta. Satelite={SateliteId}", sateliteId);
                return ResultadoOperacao<Alerta>.Falha(falhaComunicacaoException.Message, 503, "FALHA_COMUNICACAO_ORBITAL");
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, "Integridade comprometida durante processamento de alerta. Arquivo={Arquivo}", integridadeException.CaminhoArquivo);
                return ResultadoOperacao<Alerta>.Falha(integridadeException.Message, 500, "INTEGRIDADE_DADOS_COMPROMETIDA");
            default:
                _logger.LogError(exception, "Erro inesperado ao processar alerta. Satelite={SateliteId}", sateliteId);
                return ResultadoOperacao<Alerta>.Falha("Erro interno ao processar alerta.", 500, "ERRO_INTERNO");
        }
    }

    private void TratarFalhaNotificacao(Exception exception, string operadora)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, "Falha ao notificar operadora por satelite nao encontrado. Satelite={SateliteId}", sateliteException.SateliteId);
                break;
            case TelemetriaInvalidaException telemetriaException:
                _logger.LogWarning(telemetriaException, "Falha ao notificar operadora por telemetria invalida. Motivo={Motivo}", telemetriaException.Motivo);
                break;
            case IntervencaoNaoAutorizadaException intervencaoException:
                _logger.LogCritical(intervencaoException, "Falha ao notificar operadora por intervencao nao autorizada. Satelite={SateliteId}, Robo={RoboId}", intervencaoException.SateliteId, intervencaoException.RoboId);
                AlertaSegurancaHelper.RegistrarEscaladaPrivilegio(_logger, intervencaoException.SateliteId, intervencaoException.RoboId);
                break;
            case RecursoOrbitalIndisponivelException recursoException:
                _logger.LogWarning(recursoException, "Falha ao notificar operadora por recurso orbital indisponivel. Tipo={TipoSolicitado}", recursoException.TipoSolicitado);
                break;
            case FalhaDeComunicacaoOrbitalException falhaComunicacaoException:
                _logger.LogError(falhaComunicacaoException, "Falha de comunicacao orbital ao notificar operadora. Operadora={Operadora}", operadora);
                break;
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, "Integridade comprometida ao notificar operadora. Arquivo={Arquivo}", integridadeException.CaminhoArquivo);
                break;
            default:
                _logger.LogError(exception, "Erro inesperado ao notificar operadora. Operadora={Operadora}", operadora);
                break;
        }
    }

    private void TratarFalhaRegistroLog(Exception exception, string sateliteId)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, "Falha ao registrar log de alerta por satelite nao encontrado. Satelite={SateliteId}", sateliteException.SateliteId);
                break;
            case TelemetriaInvalidaException telemetriaException:
                _logger.LogWarning(telemetriaException, "Falha ao registrar log de alerta por telemetria invalida. Motivo={Motivo}", telemetriaException.Motivo);
                break;
            case IntervencaoNaoAutorizadaException intervencaoException:
                _logger.LogCritical(intervencaoException, "Falha ao registrar log de alerta por intervencao nao autorizada. Satelite={SateliteId}, Robo={RoboId}", intervencaoException.SateliteId, intervencaoException.RoboId);
                AlertaSegurancaHelper.RegistrarEscaladaPrivilegio(_logger, intervencaoException.SateliteId, intervencaoException.RoboId);
                break;
            case RecursoOrbitalIndisponivelException recursoException:
                _logger.LogWarning(recursoException, "Falha ao registrar log de alerta por recurso orbital indisponivel. Tipo={TipoSolicitado}", recursoException.TipoSolicitado);
                break;
            case FalhaDeComunicacaoOrbitalException falhaComunicacaoException:
                _logger.LogError(falhaComunicacaoException, "Falha de comunicacao orbital ao registrar log de alerta. Satelite={SateliteId}", sateliteId);
                break;
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, "Integridade comprometida ao registrar log de alerta. Arquivo={Arquivo}", integridadeException.CaminhoArquivo);
                break;
            default:
                _logger.LogError(exception, "Erro inesperado ao registrar log de alerta. Satelite={SateliteId}", sateliteId);
                break;
        }
    }

    public ResultadoOperacao<IReadOnlyCollection<Alerta>> ListarAlertasPorOperadora(string operadora)
    {
        try
        {
            var satelitesDaOperadora = _repositorioSatelite.ListarTodos()
                .Where(satelite => satelite.Operadora.Equals(operadora, StringComparison.OrdinalIgnoreCase))
                .Select(satelite => satelite.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var alertas = _alertasRegistrados
                .Where(alerta => satelitesDaOperadora.Contains(alerta.SateliteId))
                .OrderByDescending(alerta => alerta.DataHoraCriacao)
                .ToList()
                .AsReadOnly();

            return ResultadoOperacao<IReadOnlyCollection<Alerta>>.Ok(alertas);
        }
        catch (SateliteNaoEncontradoException exception)
        {
            _logger.LogError(exception, "Erro ao listar alertas por operadora. Operadora={Operadora}", operadora);
            return ResultadoOperacao<IReadOnlyCollection<Alerta>>.Falha(exception.Message, 404, "SATELITE_NAO_ENCONTRADO");
        }
        catch (IntegridadeDadosComprometidaException exception)
        {
            _logger.LogCritical(exception, "Integridade comprometida ao listar alertas por operadora. Operadora={Operadora}", operadora);
            return ResultadoOperacao<IReadOnlyCollection<Alerta>>.Falha(exception.Message, 500, "INTEGRIDADE_DADOS_COMPROMETIDA");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro inesperado ao listar alertas por operadora. Operadora={Operadora}", operadora);
            return ResultadoOperacao<IReadOnlyCollection<Alerta>>.Falha("Erro ao listar alertas da operadora.", 500, "ERRO_INTERNO");
        }
    }

    public ResultadoOperacao<IReadOnlyCollection<Alerta>> ListarAlertasPorSatelite(string sateliteId, string operadora)
    {
        try
        {
            var satelite = _repositorioSatelite.ObterPorId(sateliteId)
                ?? throw new SateliteNaoEncontradoException(sateliteId);

            if (!satelite.Operadora.Equals(operadora, StringComparison.OrdinalIgnoreCase))
            {
                return ResultadoOperacao<IReadOnlyCollection<Alerta>>.Falha(
                    "Operadora nao autorizada para consultar este satelite.",
                    403,
                    "OPERADORA_NAO_AUTORIZADA");
            }

            var alertas = _alertasRegistrados
                .Where(alerta => alerta.SateliteId.Equals(sateliteId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(alerta => alerta.DataHoraCriacao)
                .ToList()
                .AsReadOnly();

            return ResultadoOperacao<IReadOnlyCollection<Alerta>>.Ok(alertas);
        }
        catch (SateliteNaoEncontradoException exception)
        {
            _logger.LogError(exception, "Erro ao listar alertas do satelite. Satelite={SateliteId}", sateliteId);
            return ResultadoOperacao<IReadOnlyCollection<Alerta>>.Falha(exception.Message, 404, "SATELITE_NAO_ENCONTRADO");
        }
        catch (IntegridadeDadosComprometidaException exception)
        {
            _logger.LogCritical(exception, "Integridade comprometida ao listar alertas do satelite. Satelite={SateliteId}", sateliteId);
            return ResultadoOperacao<IReadOnlyCollection<Alerta>>.Falha(exception.Message, 500, "INTEGRIDADE_DADOS_COMPROMETIDA");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro inesperado ao listar alertas do satelite. Satelite={SateliteId}", sateliteId);
            return ResultadoOperacao<IReadOnlyCollection<Alerta>>.Falha("Erro ao listar alertas do satelite.", 500, "ERRO_INTERNO");
        }
    }
}
