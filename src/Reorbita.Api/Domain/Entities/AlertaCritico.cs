using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Entities;

public sealed class AlertaCritico : Alerta
{
    public AlertaCritico(string sateliteId, string? descricao = null)
        : base(
            sateliteId,
            TipoAlerta.Critico,
            string.IsNullOrWhiteSpace(descricao)
                ? "Falha critica detectada. Intervencao imediata requerida."
                : descricao)
    {
    }

    public override string GerarDescricao()
    {
        return Descricao;
    }

    public override string DefinirAcaoRecomendada()
    {
        return "Acionar ServicoFrota para intervencao automatica.";
    }
}
