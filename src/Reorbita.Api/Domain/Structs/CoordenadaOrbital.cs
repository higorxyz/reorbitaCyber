namespace Reorbita.Api.Domain.Structs;

public readonly record struct CoordenadaOrbital(
    double Altitude,
    double Inclinacao,
    double ExcentricidadeOrbital,
    double AscensaoRetaDireta
);
