using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Models.Responses;

public sealed class AlertaResponse
{
    public required string SateliteId { get; init; }

    public required DateTime DataHoraCriacao { get; init; }

    public required TipoAlerta TipoAlerta { get; init; }

    public required string Descricao { get; init; }

    public required string AcaoRecomendada { get; init; }
}
