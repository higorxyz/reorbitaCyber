using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Interfaces;

public interface IAlertavel
{
    Alerta GerarAlerta(TipoAlerta tipoAlerta, string descricao);

    void ReceberAlerta(Alerta alerta);
}
