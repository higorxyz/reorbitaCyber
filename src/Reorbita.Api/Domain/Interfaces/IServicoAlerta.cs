using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Interfaces;

public interface IServicoAlerta
{
    ResultadoOperacao<Alerta> ProcessarAlerta(Alerta alerta);

    void NotificarOperadora(string operadora, string mensagem);

    void RegistrarNoLog(Alerta alerta);

    ResultadoOperacao<IReadOnlyCollection<Alerta>> ListarAlertasPorOperadora(string operadora);

    ResultadoOperacao<IReadOnlyCollection<Alerta>> ListarAlertasPorSatelite(string sateliteId, string operadora);
}
