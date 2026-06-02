namespace Reorbita.Api.Domain.Structs;

public readonly record struct LeituraTelemetria(
    string SensorId,
    double Valor,
    string Unidade,
    DateTime DataHoraColeta,
    bool DentroDoLimiteNormal
);
