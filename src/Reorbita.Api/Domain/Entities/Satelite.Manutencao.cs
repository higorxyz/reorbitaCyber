using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public abstract partial class Satelite
{
    public ResultadoIntervencao ExecutarIntervencao(TipoIntervencao tipoIntervencao, string roboId, NivelAcesso nivelAcesso)
    {
        if (nivelAcesso is NivelAcesso.OperadoraLeitura)
        {
            throw new IntervencaoNaoAutorizadaException(Id, roboId);
        }

        if (!VerificarCompatibilidade(tipoIntervencao))
        {
            throw new IntervencaoNaoAutorizadaException(Id, roboId);
        }

        switch (tipoIntervencao)
        {
            case TipoIntervencao.Reabastecimento:
                AtualizarNivelCombustivel(NivelCombustivel + 35.0);
                break;
            case TipoIntervencao.TrocaModuloEletronico:
                AtualizarDegradacaoBateria(DegradacaoBateria - 20.0);
                break;
            case TipoIntervencao.CorrecaoTrajetoria:
                OrbitaAtual = OrbitaAtual with
                {
                    ExcentricidadeOrbital = Math.Max(0.0, OrbitaAtual.ExcentricidadeOrbital - 0.001)
                };
                break;
            case TipoIntervencao.CapturaDetritos:
                AtualizarDegradacaoBateria(DegradacaoBateria - 5.0);
                break;
        }

        var resultadoIntervencao = new ResultadoIntervencao(
            true,
            $"Intervencao '{tipoIntervencao}' executada pelo robo '{roboId}'.",
            DateTime.UtcNow,
            tipoIntervencao);

        RegistrarManutencaoRealizada(resultadoIntervencao);
        AvaliarSaude();
        return resultadoIntervencao;
    }

    public bool VerificarCompatibilidade(TipoIntervencao tipoIntervencao)
    {
        if (StatusAtual is StatusSatelite.Inoperante)
        {
            return tipoIntervencao is TipoIntervencao.Reabastecimento or TipoIntervencao.TrocaModuloEletronico;
        }

        return true;
    }

    public void RegistrarManutencaoRealizada(ResultadoIntervencao resultadoIntervencao)
    {
        AdicionarHistoricoManutencao(resultadoIntervencao);
    }
}
