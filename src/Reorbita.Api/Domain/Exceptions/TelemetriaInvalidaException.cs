namespace Reorbita.Api.Domain.Exceptions;

public sealed class TelemetriaInvalidaException : Exception
{
    public TelemetriaInvalidaException(string motivo)
        : base($"Leitura de telemetria invalida: {motivo}")
    {
        Motivo = motivo;
    }

    public string Motivo { get; }
}
