using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public sealed class SateliteComercial : Satelite
{
    public SateliteComercial(
        string id,
        string nome,
        string operadora,
        CoordenadaOrbital orbitaAtual,
        DateTime dataLancamento,
        StatusSatelite statusAtual,
        double nivelCombustivelInicial,
        double degradacaoBateriaInicial,
        int slaContratado)
        : base(id, nome, operadora, orbitaAtual, dataLancamento, statusAtual, nivelCombustivelInicial, degradacaoBateriaInicial)
    {
        SlaContratado = Math.Clamp(slaContratado, 1, 5);
    }

    public int SlaContratado { get; }

    public override int CalcularPrioridadeIntervencao()
    {
        var basePrioridade = StatusAtual switch
        {
            StatusSatelite.FalhaIminente => 95,
            StatusSatelite.CriticoAtencao => 80,
            StatusSatelite.Degradado => 60,
            _ => 40
        };

        return Math.Min(100, basePrioridade + (SlaContratado * 2));
    }
}
