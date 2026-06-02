using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Models.Requests;

public sealed class CriarSateliteRequest
{
    public required string TipoSatelite { get; init; }

    public required string Id { get; init; }

    public required string Nome { get; init; }

    public required string Operadora { get; init; }

    public required CoordenadaOrbital OrbitaAtual { get; init; }

    public required DateTime DataLancamentoUtc { get; init; }

    public required StatusSatelite StatusInicial { get; init; }

    public required double NivelCombustivelInicial { get; init; }

    public required double DegradacaoBateriaInicial { get; init; }

    public int? SlaContratado { get; init; }

    public bool? MissaoAtiva { get; init; }
}
