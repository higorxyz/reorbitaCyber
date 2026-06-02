using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public sealed class RoboReparador : RoboOrbital
{
    public RoboReparador(string id, bool disponivel, string estacaoMaeId)
        : base(id, TipoIntervencao.TrocaModuloEletronico, disponivel, estacaoMaeId)
    {
    }

    public override ResultadoIntervencao ExecutarIntervencao(string sateliteId, TipoIntervencao tipoIntervencao)
    {
        if (!VerificarCompatibilidade(tipoIntervencao))
        {
            throw new IntervencaoNaoAutorizadaException(sateliteId, Id);
        }

        AtualizarDisponibilidade(false);
        return new ResultadoIntervencao(true, "Reparo de modulo concluido.", DateTime.UtcNow, tipoIntervencao);
    }
}
