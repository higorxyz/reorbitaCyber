using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Interfaces;

public interface IServicoFrota
{
    ResultadoOperacao<OrdemServico> SolicitarIntervencao(string sateliteId, TipoIntervencao tipoIntervencao, string operadoraSolicitante, NivelAcesso nivelAcesso);

    ResultadoOperacao<IReadOnlyCollection<OrdemServico>> ListarOrdens();

    ResultadoOperacao<IReadOnlyCollection<RoboOrbital>> ListarRobos();
}
