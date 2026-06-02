namespace Reorbita.Api.Domain.Entities;

public sealed class SensorBateria : Sensor
{
    public SensorBateria(string id)
        : base(id, "%", 10.0, 100.0)
    {
    }

    public override Domain.Structs.LeituraTelemetria ColetarLeitura()
    {
        var valorSimulado = Random.Shared.NextDouble() * 100.0;
        return CriarLeitura(valorSimulado);
    }
}
