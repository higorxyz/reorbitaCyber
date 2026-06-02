namespace Reorbita.Api.Domain.Entities;

public sealed class SensorPropulsao : Sensor
{
    public SensorPropulsao(string id)
        : base(id, "kN", 30.0, 100.0)
    {
    }

    public override Domain.Structs.LeituraTelemetria ColetarLeitura()
    {
        var valorSimulado = 20.0 + (Random.Shared.NextDouble() * 90.0);
        return CriarLeitura(valorSimulado);
    }
}
