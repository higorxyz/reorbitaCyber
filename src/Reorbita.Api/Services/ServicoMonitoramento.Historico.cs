using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Services;

public sealed partial class ServicoMonitoramento
{
    public ResultadoOperacao<IReadOnlyCollection<LeituraTelemetria>> ObterHistoricoTelemetria(string sateliteId, DateTime? dataInicioUtc, DateTime? dataFimUtc)
    {
        try
        {
            var satelite = _repositorioSatelite.ObterPorId(sateliteId)
                ?? throw new SateliteNaoEncontradoException(sateliteId);

            var historico = satelite.ObterHistoricoTelemetria(dataInicioUtc, dataFimUtc);
            return ResultadoOperacao<IReadOnlyCollection<LeituraTelemetria>>.Ok(historico);
        }
        catch (Exception exception)
        {
            return TratarFalhaComSatelite<IReadOnlyCollection<LeituraTelemetria>>(
                exception,
                sateliteId,
                "Satelite nao encontrado ao consultar historico. Satelite={SateliteId}",
                "Integridade comprometida ao consultar historico do satelite. Satelite={SateliteId}",
                "Erro ao consultar historico do satelite. Satelite={SateliteId}",
                "Erro ao consultar historico de telemetria.");
        }
    }

    public ResultadoOperacao<IReadOnlyCollection<Satelite>> ListarSatelitesPorOperadora(string operadora)
    {
        try
        {
            var satelites = _repositorioSatelite.ListarTodos()
                .Where(satelite => satelite.Operadora.Equals(operadora, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();

            return ResultadoOperacao<IReadOnlyCollection<Satelite>>.Ok(satelites);
        }
        catch (Exception exception)
        {
            return TratarFalhaGenerica<IReadOnlyCollection<Satelite>>(
                exception,
                operadora,
                "Integridade comprometida ao listar satelites da operadora. Operadora={Operadora}",
                "Erro ao listar satelites da operadora. Operadora={Operadora}",
                "Erro ao listar satelites.");
        }
    }

    public ResultadoOperacao<Satelite> ObterSatelitePorId(string id, string operadora)
    {
        try
        {
            var satelite = _repositorioSatelite.ObterPorId(id)
                ?? throw new SateliteNaoEncontradoException(id);

            if (!satelite.Operadora.Equals(operadora, StringComparison.OrdinalIgnoreCase))
            {
                return ResultadoOperacao<Satelite>.Falha("Operadora nao autorizada para este satelite.", 403, "OPERADORA_NAO_AUTORIZADA");
            }

            return ResultadoOperacao<Satelite>.Ok(satelite);
        }
        catch (Exception exception)
        {
            return TratarFalhaComSatelite<Satelite>(
                exception,
                id,
                "Satelite nao encontrado. Satelite={SateliteId}",
                "Integridade comprometida ao obter satelite por ID {SateliteId}",
                "Erro ao obter satelite por ID {SateliteId}",
                "Erro ao obter satelite.");
        }
    }

    public ResultadoOperacao<Satelite> CadastrarSatelite(Satelite satelite)
    {
        try
        {
            _repositorioSatelite.Salvar(satelite);
            return ResultadoOperacao<Satelite>.Ok(satelite, "Satelite cadastrado com sucesso.", 201);
        }
        catch (Exception exception)
        {
            return TratarFalhaGenerica<Satelite>(
                exception,
                satelite.Id,
                "Integridade comprometida ao cadastrar satelite. Satelite={SateliteId}",
                "Erro ao cadastrar satelite. Satelite={SateliteId}",
                "Erro ao cadastrar satelite.");
        }
    }

    public ResultadoOperacao<Satelite> AtualizarSatelite(string id, Satelite sateliteAtualizado, string operadora)
    {
        try
        {
            var sateliteExistente = _repositorioSatelite.ObterPorId(id)
                ?? throw new SateliteNaoEncontradoException(id);

            if (!sateliteExistente.Operadora.Equals(operadora, StringComparison.OrdinalIgnoreCase))
            {
                return ResultadoOperacao<Satelite>.Falha("Operadora nao autorizada para atualizar este satelite.", 403, "OPERADORA_NAO_AUTORIZADA");
            }

            _repositorioSatelite.Atualizar(sateliteAtualizado);
            return ResultadoOperacao<Satelite>.Ok(sateliteAtualizado, "Satelite atualizado com sucesso.");
        }
        catch (Exception exception)
        {
            return TratarFalhaComSatelite<Satelite>(
                exception,
                id,
                "Satelite nao encontrado para atualizacao. Satelite={SateliteId}",
                "Integridade comprometida ao atualizar satelite. Satelite={SateliteId}",
                "Erro ao atualizar satelite. Satelite={SateliteId}",
                "Erro ao atualizar satelite.");
        }
    }

    private ResultadoOperacao<T> TratarFalhaComSatelite<T>(
        Exception exception,
        string sateliteId,
        string mensagemSateliteNaoEncontradoLog,
        string mensagemIntegridadeLog,
        string mensagemErroLog,
        string mensagemErroInterno)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, mensagemSateliteNaoEncontradoLog, sateliteException.SateliteId);
                return ResultadoOperacao<T>.Falha(sateliteException.Message, 404, "SATELITE_NAO_ENCONTRADO");
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, mensagemIntegridadeLog, sateliteId);
                return ResultadoOperacao<T>.Falha(integridadeException.Message, 500, "INTEGRIDADE_DADOS_COMPROMETIDA");
            default:
                _logger.LogError(exception, mensagemErroLog, sateliteId);
                return ResultadoOperacao<T>.Falha(mensagemErroInterno, 500, "ERRO_INTERNO");
        }
    }

    private ResultadoOperacao<T> TratarFalhaGenerica<T>(
        Exception exception,
        string contexto,
        string mensagemIntegridadeLog,
        string mensagemErroLog,
        string mensagemErroInterno)
    {
        switch (exception)
        {
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, mensagemIntegridadeLog, contexto);
                return ResultadoOperacao<T>.Falha(integridadeException.Message, 500, "INTEGRIDADE_DADOS_COMPROMETIDA");
            default:
                _logger.LogError(exception, mensagemErroLog, contexto);
                return ResultadoOperacao<T>.Falha(mensagemErroInterno, 500, "ERRO_INTERNO");
        }
    }
}
