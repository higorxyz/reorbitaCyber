using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Models.Requests;

public sealed class SolicitarTokenJwtRequest
{
    public required string UsuarioId { get; init; }

    public required string Operadora { get; init; }

    public required NivelAcesso NivelAcesso { get; init; }

    public bool MfaHabilitado { get; init; }

    public string? CodigoAcesso { get; init; }
}
