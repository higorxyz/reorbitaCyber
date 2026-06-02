using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Models.Responses;

public sealed class RelatorioSaudeResponse
{
    public required string SateliteId { get; init; }

    public required string NomeSatelite { get; init; }

    public required StatusSatelite StatusAtual { get; init; }

    public required DateTime DataHoraAtualizacao { get; init; }

    public required double NivelCombustivel { get; init; }

    public required double DegradacaoBateria { get; init; }

    public required IReadOnlyCollection<AlertaResponse> AlertasAtivos { get; init; }
}
