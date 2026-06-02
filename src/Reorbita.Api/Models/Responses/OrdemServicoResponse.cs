using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Models.Responses;

public sealed class OrdemServicoResponse
{
    public required string Id { get; init; }

    public required string SateliteId { get; init; }

    public required string RoboId { get; init; }

    public required TipoIntervencao TipoIntervencao { get; init; }

    public required DateTime DataHoraAgendada { get; init; }

    public required string StatusOrdem { get; init; }

    public DateTime? DataHoraConclusao { get; init; }
}
