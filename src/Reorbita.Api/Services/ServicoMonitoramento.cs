using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;
using Reorbita.Api.Infrastructure;

namespace Reorbita.Api.Services;

public sealed partial class ServicoMonitoramento : IServicoMonitoramento
{
    private readonly IRepositorioSatelite _repositorioSatelite;
    private readonly IMotorPreditivo _motorPreditivo;
    private readonly IServicoAlerta _servicoAlerta;
    private readonly ILogger<ServicoMonitoramento> _logger;

    public ServicoMonitoramento(
        IRepositorioSatelite repositorioSatelite,
        IMotorPreditivo motorPreditivo,
        IServicoAlerta servicoAlerta,
        ILogger<ServicoMonitoramento> logger)
    {
        _repositorioSatelite = repositorioSatelite;
        _motorPreditivo = motorPreditivo;
        _servicoAlerta = servicoAlerta;
        _logger = logger;
    }

    public ResultadoOperacao<RelatorioSaude> ReceberTelemetria(string sateliteId, LeituraTelemetria leituraTelemetria)
    {
        try
        {
            var satelite = _repositorioSatelite.ObterPorId(sateliteId)
                ?? throw new SateliteNaoEncontradoException(sateliteId);

            ValidarLeituraTelemetria(leituraTelemetria);

            var leituraComTimestampUtc = leituraTelemetria with
            {
                DataHoraColeta = leituraTelemetria.DataHoraColeta.Kind == DateTimeKind.Utc
                    ? leituraTelemetria.DataHoraColeta
                    : DateTime.SpecifyKind(leituraTelemetria.DataHoraColeta, DateTimeKind.Utc)
            };
            satelite.RegistrarLeituraTelemetria(leituraComTimestampUtc);

            var alertasGerados = ProcessarAnalisePreditiva(satelite, leituraComTimestampUtc);
            AtualizarStatusPosAnalise(satelite, alertasGerados);

            _repositorioSatelite.Atualizar(satelite);

            var relatorioSaude = new RelatorioSaude
            {
                SateliteId = satelite.Id,
                NomeSatelite = satelite.Nome,
                StatusAtual = satelite.StatusAtual,
                DataHoraAtualizacao = DateTime.UtcNow,
                NivelCombustivel = satelite.NivelCombustivel,
                DegradacaoBateria = satelite.DegradacaoBateria,
                AlertasAtivos = alertasGerados
            };

            return ResultadoOperacao<RelatorioSaude>.Ok(relatorioSaude, "Telemetria processada com sucesso.");
        }
        catch (SateliteNaoEncontradoException exception)
        {
            _logger.LogError(exception, "Satelite nao encontrado no recebimento de telemetria. Satelite={SateliteId}", exception.SateliteId);
            return ResultadoOperacao<RelatorioSaude>.Falha(exception.Message, 404, "SATELITE_NAO_ENCONTRADO");
        }
        catch (TelemetriaInvalidaException exception)
        {
            _logger.LogWarning(exception, "Telemetria invalida no recebimento. Satelite={SateliteId}, Motivo={Motivo}", sateliteId, exception.Motivo);
            return ResultadoOperacao<RelatorioSaude>.Falha(exception.Message, 400, "TELEMETRIA_INVALIDA");
        }
        catch (IntervencaoNaoAutorizadaException exception)
        {
            _logger.LogCritical(exception, "Intervencao nao autorizada detectada durante monitoramento. Satelite={SateliteId}, Robo={RoboId}", exception.SateliteId, exception.RoboId);
            AlertaSegurancaHelper.RegistrarEscaladaPrivilegio(_logger, exception.SateliteId, exception.RoboId);
            return ResultadoOperacao<RelatorioSaude>.Falha(exception.Message, 403, "INTERVENCAO_NAO_AUTORIZADA");
        }
        catch (RecursoOrbitalIndisponivelException exception)
        {
            _logger.LogWarning(exception, "Recurso orbital indisponivel durante monitoramento. Tipo={TipoIntervencao}", exception.TipoSolicitado);
            return ResultadoOperacao<RelatorioSaude>.Falha(exception.Message, 409, "RECURSO_ORBITAL_INDISPONIVEL");
        }
        catch (FalhaDeComunicacaoOrbitalException exception)
        {
            _logger.LogError(exception, "Falha de comunicacao orbital no recebimento de telemetria. Satelite={SateliteId}", sateliteId);
            return ResultadoOperacao<RelatorioSaude>.Falha(exception.Message, 503, "FALHA_COMUNICACAO_ORBITAL");
        }
        catch (IntegridadeDadosComprometidaException exception)
        {
            _logger.LogCritical(exception, "Integridade comprometida no monitoramento. Arquivo={Arquivo}", exception.CaminhoArquivo);
            return ResultadoOperacao<RelatorioSaude>.Falha(exception.Message, 500, "INTEGRIDADE_DADOS_COMPROMETIDA");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro inesperado ao processar telemetria. Satelite={SateliteId}", sateliteId);
            return ResultadoOperacao<RelatorioSaude>.Falha("Erro interno ao processar telemetria.", 500, "ERRO_INTERNO");
        }
    }

    private static void ValidarLeituraTelemetria(LeituraTelemetria leituraTelemetria)
    {
        if (string.IsNullOrWhiteSpace(leituraTelemetria.SensorId))
        {
            throw new TelemetriaInvalidaException("SensorId nao pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(leituraTelemetria.Unidade))
        {
            throw new TelemetriaInvalidaException("Unidade da leitura nao pode ser vazia.");
        }

        if (leituraTelemetria.DataHoraColeta > DateTime.UtcNow.AddMinutes(5))
        {
            throw new TelemetriaInvalidaException("DataHoraColeta nao pode ser futura.");
        }

        var sensorNormalizado = leituraTelemetria.SensorId.Trim().ToLowerInvariant();
        if (sensorNormalizado.Contains("bateria") && (leituraTelemetria.Valor < 0 || leituraTelemetria.Valor > 100))
        {
            throw new TelemetriaInvalidaException("Bateria fora de faixa fisica esperada (0..100%).");
        }

        if (sensorNormalizado.Contains("propulsao") && (leituraTelemetria.Valor < 0 || leituraTelemetria.Valor > 200))
        {
            throw new TelemetriaInvalidaException("Propulsao fora de faixa fisica esperada (0..200 kN).");
        }

        if (sensorNormalizado.Contains("painel") && (leituraTelemetria.Valor < 0 || leituraTelemetria.Valor > 100))
        {
            throw new TelemetriaInvalidaException("Painel solar fora de faixa fisica esperada (0..100%).");
        }

        if (sensorNormalizado.Contains("orbitacao") && (leituraTelemetria.Valor < 100 || leituraTelemetria.Valor > 2000))
        {
            throw new TelemetriaInvalidaException("Orbita fora de faixa fisica esperada (100..2000 km).");
        }
    }
}
