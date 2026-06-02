using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Structs;

public readonly record struct SolicitacaoTokenAcesso(
    string UsuarioId,
    string Operadora,
    NivelAcesso NivelAcesso,
    bool MfaHabilitado,
    string? CodigoAcesso
);
