using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public sealed class SateliteDefesa : Satelite
{
    public SateliteDefesa(
        string id,
        string nome,
        string operadora,
        CoordenadaOrbital orbitaAtual,
        DateTime dataLancamento,
        StatusSatelite statusAtual,
        double nivelCombustivelInicial,
        double degradacaoBateriaInicial)
        : base(id, nome, operadora, orbitaAtual, dataLancamento, statusAtual, nivelCombustivelInicial, degradacaoBateriaInicial)
    {
    }

    public override int CalcularPrioridadeIntervencao()
    {
        return 100;
    }
}
