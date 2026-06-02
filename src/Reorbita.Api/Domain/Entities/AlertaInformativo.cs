using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Entities;

public sealed class AlertaInformativo : Alerta
{
    public AlertaInformativo(string sateliteId, string? descricao = null)
        : base(
            sateliteId,
            TipoAlerta.Informativo,
            string.IsNullOrWhiteSpace(descricao)
                ? "Evento informativo registrado para acompanhamento da equipe."
                : descricao)
    {
    }

    public override string GerarDescricao()
    {
        return Descricao;
    }

    public override string DefinirAcaoRecomendada()
    {
        return "Registrar no log e acompanhar tendencia.";
    }
}
