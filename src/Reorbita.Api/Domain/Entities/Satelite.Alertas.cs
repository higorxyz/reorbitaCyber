using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Entities;

public abstract partial class Satelite
{
    public Alerta GerarAlerta(TipoAlerta tipoAlerta, string descricao)
    {
        Alerta alerta = tipoAlerta switch
        {
            TipoAlerta.Critico => new AlertaCritico(Id, descricao),
            TipoAlerta.Preventivo => new AlertaPreventivo(Id, descricao),
            _ => new AlertaInformativo(Id, descricao)
        };

        ReceberAlerta(alerta);
        return alerta;
    }

    public void ReceberAlerta(Alerta alerta)
    {
        AdicionarAlertaRecebido(alerta);

        if (alerta.TipoAlerta is TipoAlerta.Critico)
        {
            StatusAtual = StatusSatelite.CriticoAtencao;
            return;
        }

        if (alerta.TipoAlerta is TipoAlerta.Preventivo &&
            alerta.Descricao.Contains("Falha projetada", StringComparison.OrdinalIgnoreCase))
        {
            StatusAtual = StatusSatelite.FalhaIminente;
        }
    }
}
