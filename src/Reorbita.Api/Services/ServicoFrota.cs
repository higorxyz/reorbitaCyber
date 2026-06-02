using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;
using Reorbita.Api.Infrastructure;

namespace Reorbita.Api.Services;

public sealed class ServicoFrota : IServicoFrota
{
    private readonly IRepositorioFrota _repositorioFrota;
    private readonly IRepositorioSatelite _repositorioSatelite;
    private readonly ILogger<ServicoFrota> _logger;
    private readonly List<OrdemServico> _ordensServico = [];

    public ServicoFrota(
        IRepositorioFrota repositorioFrota,
        IRepositorioSatelite repositorioSatelite,
        ILogger<ServicoFrota> logger)
    {
        _repositorioFrota = repositorioFrota;
        _repositorioSatelite = repositorioSatelite;
        _logger = logger;
    }

    public ResultadoOperacao<OrdemServico> SolicitarIntervencao(string sateliteId, TipoIntervencao tipoIntervencao, string operadoraSolicitante, NivelAcesso nivelAcesso)
    {
        try
        {
            if (nivelAcesso is NivelAcesso.OperadoraLeitura)
            {
                throw new IntervencaoNaoAutorizadaException(sateliteId, "SEM-ROBO");
            }

            var satelite = _repositorioSatelite.ObterPorId(sateliteId)
                ?? throw new SateliteNaoEncontradoException(sateliteId);

            if (!satelite.Operadora.Equals(operadoraSolicitante, StringComparison.OrdinalIgnoreCase) &&
                nivelAcesso is not NivelAcesso.ReorbitaAdmin)
            {
                throw new IntervencaoNaoAutorizadaException(sateliteId, "SEM-ROBO");
            }

            var roboDisponivel = _repositorioFrota.ObterDisponivelPara(tipoIntervencao)
                ?? throw new RecursoOrbitalIndisponivelException(tipoIntervencao);

            if (!roboDisponivel.VerificarCompatibilidade(tipoIntervencao))
            {
                throw new IntervencaoNaoAutorizadaException(sateliteId, roboDisponivel.Id);
            }

            var resultadoRobo = roboDisponivel.ExecutarIntervencao(satelite.Id, tipoIntervencao);
            satelite.ExecutarIntervencao(tipoIntervencao, roboDisponivel.Id, nivelAcesso);

            var ordemServico = new OrdemServico
            {
                Id = $"OS-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                SateliteId = satelite.Id,
                RoboId = roboDisponivel.Id,
                TipoIntervencao = tipoIntervencao,
                DataHoraAgendada = DateTime.UtcNow
            };

            _ordensServico.Add(ordemServico);
            roboDisponivel.AtualizarDisponibilidade(false);

            _repositorioFrota.Atualizar(roboDisponivel);
            _repositorioSatelite.Atualizar(satelite);

            _logger.LogInformation(
                "Intervencao agendada com sucesso. Ordem={OrdemId}, Satelite={SateliteId}, Robo={RoboId}, Resultado={Mensagem}",
                ordemServico.Id,
                satelite.Id,
                roboDisponivel.Id,
                resultadoRobo.Mensagem);

            return ResultadoOperacao<OrdemServico>.Ok(ordemServico, "Intervencao agendada com sucesso.", 201);
        }
        catch (Exception exception)
        {
            return TratarFalhaSolicitacaoIntervencao(exception, sateliteId);
        }
    }

    private ResultadoOperacao<OrdemServico> TratarFalhaSolicitacaoIntervencao(Exception exception, string sateliteId)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, "Satelite nao encontrado ao solicitar intervencao. Satelite={SateliteId}", sateliteException.SateliteId);
                return ResultadoOperacao<OrdemServico>.Falha(sateliteException.Message, 404, "SATELITE_NAO_ENCONTRADO");
            case TelemetriaInvalidaException telemetriaException:
                _logger.LogWarning(telemetriaException, "Telemetria invalida durante solicitacao de intervencao. Motivo={Motivo}", telemetriaException.Motivo);
                return ResultadoOperacao<OrdemServico>.Falha(telemetriaException.Message, 400, "TELEMETRIA_INVALIDA");
            case IntervencaoNaoAutorizadaException intervencaoException:
                _logger.LogCritical(intervencaoException, "Intervencao nao autorizada. Satelite={SateliteId}, Robo={RoboId}", intervencaoException.SateliteId, intervencaoException.RoboId);
                AlertaSegurancaHelper.RegistrarEscaladaPrivilegio(_logger, intervencaoException.SateliteId, intervencaoException.RoboId);
                return ResultadoOperacao<OrdemServico>.Falha(intervencaoException.Message, 403, "INTERVENCAO_NAO_AUTORIZADA");
            case RecursoOrbitalIndisponivelException recursoException:
                _logger.LogWarning(recursoException, "Recurso orbital indisponivel para tipo de intervencao. Tipo={TipoIntervencao}", recursoException.TipoSolicitado);
                return ResultadoOperacao<OrdemServico>.Falha(recursoException.Message, 409, "RECURSO_ORBITAL_INDISPONIVEL");
            case FalhaDeComunicacaoOrbitalException falhaComunicacaoException:
                _logger.LogError(falhaComunicacaoException, "Falha de comunicacao orbital ao solicitar intervencao. Satelite={SateliteId}", sateliteId);
                return ResultadoOperacao<OrdemServico>.Falha(falhaComunicacaoException.Message, 503, "FALHA_COMUNICACAO_ORBITAL");
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, "Integridade comprometida em arquivo de frota. Arquivo={Arquivo}", integridadeException.CaminhoArquivo);
                return ResultadoOperacao<OrdemServico>.Falha(integridadeException.Message, 500, "INTEGRIDADE_DADOS_COMPROMETIDA");
            default:
                _logger.LogError(exception, "Erro inesperado ao solicitar intervencao. Satelite={SateliteId}", sateliteId);
                return ResultadoOperacao<OrdemServico>.Falha("Erro interno ao processar solicitacao de intervencao.", 500, "ERRO_INTERNO");
        }
    }

    public ResultadoOperacao<IReadOnlyCollection<OrdemServico>> ListarOrdens()
    {
        try
        {
            return ResultadoOperacao<IReadOnlyCollection<OrdemServico>>.Ok(_ordensServico.AsReadOnly());
        }
        catch (IntegridadeDadosComprometidaException exception)
        {
            _logger.LogCritical(exception, "Integridade comprometida ao listar ordens.");
            return ResultadoOperacao<IReadOnlyCollection<OrdemServico>>.Falha(exception.Message, 500, "INTEGRIDADE_DADOS_COMPROMETIDA");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro ao listar ordens de servico.");
            return ResultadoOperacao<IReadOnlyCollection<OrdemServico>>.Falha("Erro ao listar ordens de servico.", 500, "ERRO_INTERNO");
        }
    }

    public ResultadoOperacao<IReadOnlyCollection<RoboOrbital>> ListarRobos()
    {
        try
        {
            var robos = _repositorioFrota.ListarTodos();
            return ResultadoOperacao<IReadOnlyCollection<RoboOrbital>>.Ok(robos);
        }
        catch (IntegridadeDadosComprometidaException exception)
        {
            _logger.LogCritical(exception, "Integridade comprometida ao listar frota de robos.");
            return ResultadoOperacao<IReadOnlyCollection<RoboOrbital>>.Falha(exception.Message, 500, "INTEGRIDADE_DADOS_COMPROMETIDA");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro ao listar frota de robos.");
            return ResultadoOperacao<IReadOnlyCollection<RoboOrbital>>.Falha("Erro ao listar frota orbital.", 500, "ERRO_INTERNO");
        }
    }
}
