using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public sealed class RoboReabastecedor : RoboOrbital
{
    public RoboReabastecedor(string id, bool disponivel, string estacaoMaeId)
        : base(id, TipoIntervencao.Reabastecimento, disponivel, estacaoMaeId)
    {
    }

    public override ResultadoIntervencao ExecutarIntervencao(string sateliteId, TipoIntervencao tipoIntervencao)
    {
        if (!VerificarCompatibilidade(tipoIntervencao))
        {
            throw new IntervencaoNaoAutorizadaException(sateliteId, Id);
        }

        AtualizarDisponibilidade(false);
        return new ResultadoIntervencao(true, "Reabastecimento executado com sucesso.", DateTime.UtcNow, tipoIntervencao);
    }
}
