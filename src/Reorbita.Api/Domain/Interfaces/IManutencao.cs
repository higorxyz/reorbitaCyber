using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Interfaces;

public interface IManutencao
{
    ResultadoIntervencao ExecutarIntervencao(TipoIntervencao tipoIntervencao, string roboId, NivelAcesso nivelAcesso);

    bool VerificarCompatibilidade(TipoIntervencao tipoIntervencao);

    void RegistrarManutencaoRealizada(ResultadoIntervencao resultadoIntervencao);
}
