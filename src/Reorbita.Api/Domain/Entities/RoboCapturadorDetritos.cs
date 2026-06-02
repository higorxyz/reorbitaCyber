using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public sealed class RoboCapturadorDetritos : RoboOrbital
{
    public RoboCapturadorDetritos(string id, bool disponivel, string estacaoMaeId)
        : base(id, TipoIntervencao.CapturaDetritos, disponivel, estacaoMaeId)
    {
    }

    public override ResultadoIntervencao ExecutarIntervencao(string sateliteId, TipoIntervencao tipoIntervencao)
    {
        if (!VerificarCompatibilidade(tipoIntervencao))
        {
            throw new IntervencaoNaoAutorizadaException(sateliteId, Id);
        }

        AtualizarDisponibilidade(false);
        return new ResultadoIntervencao(true, "Captura de detritos finalizada.", DateTime.UtcNow, tipoIntervencao);
    }
}
