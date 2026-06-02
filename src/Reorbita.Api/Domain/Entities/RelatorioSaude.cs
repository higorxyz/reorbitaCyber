using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Entities;

public sealed class RelatorioSaude
{
    public required string SateliteId { get; init; }

    public required string NomeSatelite { get; init; }

    public required StatusSatelite StatusAtual { get; init; }

    public required DateTime DataHoraAtualizacao { get; init; }

    public required double NivelCombustivel { get; init; }

    public required double DegradacaoBateria { get; init; }

    public required IReadOnlyCollection<Alerta> AlertasAtivos { get; init; }
}
