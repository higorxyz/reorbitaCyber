using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public abstract partial class Satelite
{
    public void RegistrarLeituraTelemetria(LeituraTelemetria leituraTelemetria)
    {
        var leituraUtc = leituraTelemetria with
        {
            DataHoraColeta = leituraTelemetria.DataHoraColeta.Kind == DateTimeKind.Utc
                ? leituraTelemetria.DataHoraColeta
                : DateTime.SpecifyKind(leituraTelemetria.DataHoraColeta, DateTimeKind.Utc)
        };

        AdicionarLeituraHistorico(leituraUtc);

        var sensorNormalizado = leituraUtc.SensorId.Trim().ToLowerInvariant();
        if (sensorNormalizado.Contains("bateria"))
        {
            AtualizarNivelCombustivel(leituraUtc.Valor);
        }

        if (sensorNormalizado.Contains("painel"))
        {
            AtualizarDegradacaoBateria(leituraUtc.Valor);
        }

        AvaliarSaude();
    }

    public IReadOnlyCollection<LeituraTelemetria> ObterHistoricoTelemetria(DateTime? dataInicioUtc, DateTime? dataFimUtc)
    {
        var query = HistoricoTelemetria.AsEnumerable();

        if (dataInicioUtc.HasValue)
        {
            query = query.Where(leitura => leitura.DataHoraColeta >= dataInicioUtc.Value);
        }

        if (dataFimUtc.HasValue)
        {
            query = query.Where(leitura => leitura.DataHoraColeta <= dataFimUtc.Value);
        }

        return query.OrderBy(leitura => leitura.DataHoraColeta).ToList();
    }

    public IReadOnlyCollection<LeituraTelemetria> ObterLeiturasUltimasHoras(int totalHoras)
    {
        var limiteUtc = DateTime.UtcNow.AddHours(-Math.Abs(totalHoras));
        return HistoricoTelemetria
            .Where(leitura => leitura.DataHoraColeta >= limiteUtc)
            .OrderBy(leitura => leitura.DataHoraColeta)
            .ToList();
    }
}
