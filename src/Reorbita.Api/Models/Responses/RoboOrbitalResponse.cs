using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Models.Responses;

public sealed class RoboOrbitalResponse
{
    public required string Id { get; init; }

    public required TipoIntervencao TipoEspecializacao { get; init; }

    public required bool Disponivel { get; init; }

    public required string EstacaoMaeId { get; init; }
}
