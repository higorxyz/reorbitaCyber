using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Structs;

public readonly record struct PrevisaoFalha(
    string SateliteId,
    TipoFalha TipoProjetado,
    DateTime DataHoraFalhaEstimada,
    double GrauDeConfianca
);
