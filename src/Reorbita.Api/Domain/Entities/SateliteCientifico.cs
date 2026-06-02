using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public sealed class SateliteCientifico : Satelite
{
    public SateliteCientifico(
        string id,
        string nome,
        string operadora,
        CoordenadaOrbital orbitaAtual,
        DateTime dataLancamento,
        StatusSatelite statusAtual,
        double nivelCombustivelInicial,
        double degradacaoBateriaInicial,
        bool missaoAtiva)
        : base(id, nome, operadora, orbitaAtual, dataLancamento, statusAtual, nivelCombustivelInicial, degradacaoBateriaInicial)
    {
        MissaoAtiva = missaoAtiva;
    }

    public bool MissaoAtiva { get; }

    public override int CalcularPrioridadeIntervencao()
    {
        var fatorMissao = MissaoAtiva ? 20 : 5;
        var prioridadeBase = StatusAtual switch
        {
            StatusSatelite.FalhaIminente => 70,
            StatusSatelite.CriticoAtencao => 55,
            StatusSatelite.Degradado => 35,
            _ => 20
        };

        return Math.Min(100, prioridadeBase + fatorMissao);
    }
}
