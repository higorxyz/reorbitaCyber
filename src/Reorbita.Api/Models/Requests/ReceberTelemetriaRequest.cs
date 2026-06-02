namespace Reorbita.Api.Models.Requests;

public sealed class ReceberTelemetriaRequest
{
    public required string SensorId { get; init; }

    public required double Valor { get; init; }

    public required string Unidade { get; init; }

    public DateTime? DataHoraColetaUtc { get; init; }
}
