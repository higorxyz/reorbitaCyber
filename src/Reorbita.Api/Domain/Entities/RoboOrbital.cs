using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public abstract class RoboOrbital
{
    protected RoboOrbital(string id, TipoIntervencao tipoEspecializacao, bool disponivel, string estacaoMaeId)
    {
        Id = id;
        TipoEspecializacao = tipoEspecializacao;
        Disponivel = disponivel;
        EstacaoMaeId = estacaoMaeId;
    }

    public string Id { get; }

    public TipoIntervencao TipoEspecializacao { get; }

    public bool Disponivel { get; private set; }

    public string EstacaoMaeId { get; }

    public abstract ResultadoIntervencao ExecutarIntervencao(string sateliteId, TipoIntervencao tipoIntervencao);

    public bool VerificarCompatibilidade(TipoIntervencao tipoIntervencao)
    {
        if (TipoEspecializacao == tipoIntervencao)
        {
            return true;
        }

        return TipoEspecializacao is TipoIntervencao.TrocaModuloEletronico &&
               tipoIntervencao is TipoIntervencao.CorrecaoTrajetoria;
    }

    public void AtualizarDisponibilidade(bool disponivel)
    {
        Disponivel = disponivel;
    }
}
