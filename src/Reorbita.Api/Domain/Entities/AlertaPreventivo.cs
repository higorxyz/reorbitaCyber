using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Entities;

public sealed class AlertaPreventivo : Alerta
{
    public AlertaPreventivo(string sateliteId, string? descricao = null)
        : base(
            sateliteId,
            TipoAlerta.Preventivo,
            string.IsNullOrWhiteSpace(descricao)
                ? "Risco moderado identificado. Revisao recomendada nas proximas janelas orbitais."
                : descricao)
    {
    }

    public override string GerarDescricao()
    {
        return Descricao;
    }

    public override string DefinirAcaoRecomendada()
    {
        return "Notificar operadora e agendar revisao tecnica.";
    }
}
