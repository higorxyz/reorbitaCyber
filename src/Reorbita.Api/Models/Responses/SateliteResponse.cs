using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Models.Responses;

public sealed class SateliteResponse
{
    public required string Id { get; init; }

    public required string TipoSatelite { get; init; }

    public required string Nome { get; init; }

    public required string Operadora { get; init; }

    public required CoordenadaOrbital OrbitaAtual { get; init; }

    public required DateTime DataLancamento { get; init; }

    public required StatusSatelite StatusAtual { get; init; }

    public required double NivelCombustivel { get; init; }

    public required double DegradacaoBateria { get; init; }

    public int? SlaContratado { get; init; }

    public bool? MissaoAtiva { get; init; }
}
