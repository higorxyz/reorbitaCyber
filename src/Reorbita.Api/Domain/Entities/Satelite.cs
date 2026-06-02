using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public abstract partial class Satelite : IMonitoravel, IManutencao, IAlertavel
{
    private readonly List<LeituraTelemetria> _historicoTelemetria = [];
    private readonly List<ResultadoIntervencao> _historicoManutencao = [];
    private readonly List<Alerta> _alertasRecebidos = [];
    private double _nivelCombustivel;
    private double _degradacaoBateria;

    protected Satelite(
        string id,
        string nome,
        string operadora,
        CoordenadaOrbital orbitaAtual,
        DateTime dataLancamento,
        StatusSatelite statusAtual,
        double nivelCombustivelInicial,
        double degradacaoBateriaInicial)
    {
        Id = id;
        Nome = nome;
        Operadora = operadora;
        OrbitaAtual = orbitaAtual;
        DataLancamento = DateTime.SpecifyKind(dataLancamento, DateTimeKind.Utc);
        StatusAtual = statusAtual;
        _nivelCombustivel = nivelCombustivelInicial;
        _degradacaoBateria = degradacaoBateriaInicial;
    }

    public string Id { get; }

    public string Nome { get; protected set; }

    public string Operadora { get; protected set; }

    public CoordenadaOrbital OrbitaAtual { get; protected set; }

    public DateTime DataLancamento { get; }

    public StatusSatelite StatusAtual { get; protected set; }

    public double NivelCombustivel => _nivelCombustivel;

    public double DegradacaoBateria => _degradacaoBateria;

    public IReadOnlyCollection<LeituraTelemetria> HistoricoTelemetria => _historicoTelemetria.AsReadOnly();

    public IReadOnlyCollection<ResultadoIntervencao> HistoricoManutencao => _historicoManutencao.AsReadOnly();

    public IReadOnlyCollection<Alerta> AlertasRecebidos => _alertasRecebidos.AsReadOnly();

    protected void AtualizarNivelCombustivel(double novoNivelCombustivel)
    {
        _nivelCombustivel = Math.Clamp(novoNivelCombustivel, 0.0, 100.0);
    }

    protected void AtualizarDegradacaoBateria(double novaDegradacaoBateria)
    {
        _degradacaoBateria = Math.Clamp(novaDegradacaoBateria, 0.0, 100.0);
    }

    protected void AdicionarLeituraHistorico(LeituraTelemetria leituraTelemetria)
    {
        _historicoTelemetria.Add(leituraTelemetria);
    }

    protected void AdicionarHistoricoManutencao(ResultadoIntervencao resultadoIntervencao)
    {
        _historicoManutencao.Add(resultadoIntervencao);
    }

    protected void AdicionarAlertaRecebido(Alerta alerta)
    {
        _alertasRecebidos.Add(alerta);
    }

    public StatusSatelite AvaliarSaude()
    {
        if (_nivelCombustivel < 10 || _degradacaoBateria > 90)
        {
            StatusAtual = StatusSatelite.FalhaIminente;
            return StatusAtual;
        }

        if (_nivelCombustivel < 20 || _degradacaoBateria > 75)
        {
            StatusAtual = StatusSatelite.CriticoAtencao;
            return StatusAtual;
        }

        if (_nivelCombustivel < 40 || _degradacaoBateria > 60)
        {
            StatusAtual = StatusSatelite.Degradado;
            return StatusAtual;
        }

        StatusAtual = StatusSatelite.Operacional;
        return StatusAtual;
    }

    public string ObterIdAtivo()
    {
        return Id;
    }

    public StatusSatelite ObterStatusAtual()
    {
        return StatusAtual;
    }

    public abstract int CalcularPrioridadeIntervencao();
}
