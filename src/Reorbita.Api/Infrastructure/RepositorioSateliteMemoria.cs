using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Infrastructure;

public sealed class RepositorioSateliteMemoria : IRepositorioSatelite
{
    private readonly Dictionary<string, Satelite> _satelites = new(StringComparer.OrdinalIgnoreCase);

    public RepositorioSateliteMemoria()
    {
        SeedDadosIniciais();
    }

    public Satelite? ObterPorId(string id)
    {
        _satelites.TryGetValue(id, out var satelite);
        return satelite;
    }

    public IReadOnlyCollection<Satelite> ListarTodos()
    {
        return _satelites.Values.ToList().AsReadOnly();
    }

    public void Salvar(Satelite satelite)
    {
        _satelites[satelite.Id] = satelite;
    }

    public void Atualizar(Satelite satelite)
    {
        _satelites[satelite.Id] = satelite;
    }

    public void Remover(string id)
    {
        _satelites.Remove(id);
    }

    private void SeedDadosIniciais()
    {
        if (_satelites.Count > 0)
        {
            return;
        }

        var sateliteComercial = new SateliteComercial(
            id: "SAT-COM-001",
            nome: "Comercial Aurora",
            operadora: "StarLink BR",
            orbitaAtual: new CoordenadaOrbital(520.0, 53.0, 0.0012, 23.0),
            dataLancamento: DateTime.UtcNow.AddYears(-2),
            statusAtual: StatusSatelite.Operacional,
            nivelCombustivelInicial: 78.0,
            degradacaoBateriaInicial: 18.0,
            slaContratado: 5);

        var sateliteCientifico = new SateliteCientifico(
            id: "SAT-CIE-001",
            nome: "Cientifico Horizonte",
            operadora: "INPE",
            orbitaAtual: new CoordenadaOrbital(680.0, 97.0, 0.0023, 135.0),
            dataLancamento: DateTime.UtcNow.AddYears(-5),
            statusAtual: StatusSatelite.Degradado,
            nivelCombustivelInicial: 31.0,
            degradacaoBateriaInicial: 63.0,
            missaoAtiva: true);

        var sateliteDefesa = new SateliteDefesa(
            id: "SAT-DEF-001",
            nome: "Defesa Sentinela",
            operadora: "FAB",
            orbitaAtual: new CoordenadaOrbital(590.0, 62.0, 0.0045, 245.0),
            dataLancamento: DateTime.UtcNow.AddYears(-3),
            statusAtual: StatusSatelite.CriticoAtencao,
            nivelCombustivelInicial: 12.0,
            degradacaoBateriaInicial: 82.0);

        // Leituras iniciais para acionar cenarios de alerta no ambiente local.
        sateliteDefesa.RegistrarLeituraTelemetria(new LeituraTelemetria("sensor-bateria", 9.0, "%", DateTime.UtcNow.AddHours(-20), false));
        sateliteDefesa.RegistrarLeituraTelemetria(new LeituraTelemetria("sensor-propulsao", 22.0, "kN", DateTime.UtcNow.AddHours(-18), false));
        sateliteDefesa.RegistrarLeituraTelemetria(new LeituraTelemetria("sensor-painel", 19.0, "%", DateTime.UtcNow.AddHours(-12), false));
        sateliteDefesa.RegistrarLeituraTelemetria(new LeituraTelemetria("sensor-orbitacao", 1320.0, "km", DateTime.UtcNow.AddHours(-6), false));

        _satelites[sateliteComercial.Id] = sateliteComercial;
        _satelites[sateliteCientifico.Id] = sateliteCientifico;
        _satelites[sateliteDefesa.Id] = sateliteDefesa;
    }
}
