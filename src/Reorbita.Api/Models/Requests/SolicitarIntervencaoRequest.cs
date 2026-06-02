using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Models.Requests;

public sealed class SolicitarIntervencaoRequest
{
    public required string SateliteId { get; init; }

    public required TipoIntervencao TipoIntervencao { get; init; }
}
