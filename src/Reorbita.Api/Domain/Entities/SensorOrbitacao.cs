namespace Reorbita.Api.Domain.Entities;

public sealed class SensorOrbitacao : Sensor
{
    public SensorOrbitacao(string id)
        : base(id, "km", 150.0, 1200.0)
    {
    }

    public override Domain.Structs.LeituraTelemetria ColetarLeitura()
    {
        var altitudeAtual = 150.0 + (Random.Shared.NextDouble() * 1200.0);
        return CriarLeitura(altitudeAtual);
    }
}
