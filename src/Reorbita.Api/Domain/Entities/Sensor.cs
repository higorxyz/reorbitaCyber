using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public abstract class Sensor
{
    private readonly double _limiteMinimo;
    private readonly double _limiteMaximo;
    private LeituraTelemetria _ultimaLeitura;

    protected Sensor(string id, string unidade, double limiteMinimo, double limiteMaximo)
    {
        Id = id;
        Unidade = unidade;
        _limiteMinimo = limiteMinimo;
        _limiteMaximo = limiteMaximo;
        _ultimaLeitura = new LeituraTelemetria(id, 0.0, unidade, DateTime.UtcNow, true);
    }

    public string Id { get; }

    public string Unidade { get; }

    public LeituraTelemetria UltimaLeitura => _ultimaLeitura;

    public abstract LeituraTelemetria ColetarLeitura();

    protected LeituraTelemetria CriarLeitura(double valor)
    {
        var dentroDoLimiteNormal = valor >= _limiteMinimo && valor <= _limiteMaximo;
        _ultimaLeitura = new LeituraTelemetria(Id, valor, Unidade, DateTime.UtcNow, dentroDoLimiteNormal);
        return _ultimaLeitura;
    }
}
