using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Entities;

public abstract class Alerta
{
    protected Alerta(string sateliteId, TipoAlerta tipoAlerta, string descricao)
    {
        SateliteId = sateliteId;
        DataHoraCriacao = DateTime.UtcNow;
        TipoAlerta = tipoAlerta;
        Descricao = descricao;
    }

    public string SateliteId { get; }

    public DateTime DataHoraCriacao { get; }

    public TipoAlerta TipoAlerta { get; }

    public string Descricao { get; }

    public abstract string GerarDescricao();

    public abstract string DefinirAcaoRecomendada();
}
