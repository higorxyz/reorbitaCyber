using Reorbita.Api.Domain.Entities;

namespace Reorbita.Api.Domain.Interfaces;

public interface IRepositorioSatelite
{
    Satelite? ObterPorId(string id);

    IReadOnlyCollection<Satelite> ListarTodos();

    void Salvar(Satelite satelite);

    void Atualizar(Satelite satelite);

    void Remover(string id);
}
