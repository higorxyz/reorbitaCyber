namespace Reorbita.Api.Domain.Entities;

public sealed class SensorPainelSolar : Sensor
{
    public SensorPainelSolar(string id)
        : base(id, "%", 0.0, 15.0)
    {
    }

    public override Domain.Structs.LeituraTelemetria ColetarLeitura()
    {
        var degradacaoAtual = Random.Shared.NextDouble() * 25.0;
        return CriarLeitura(degradacaoAtual);
    }
}
