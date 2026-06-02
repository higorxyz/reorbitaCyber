using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Interfaces;

public interface IRepositorioFrota
{
    RoboOrbital? ObterDisponivelPara(TipoIntervencao tipoIntervencao);

    IReadOnlyCollection<RoboOrbital> ListarTodos();

    void Atualizar(RoboOrbital roboOrbital);
}
