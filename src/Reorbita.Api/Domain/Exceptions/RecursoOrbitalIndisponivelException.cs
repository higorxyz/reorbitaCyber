using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Exceptions;

public sealed class RecursoOrbitalIndisponivelException : Exception
{
    public RecursoOrbitalIndisponivelException(TipoIntervencao tipoSolicitado)
        : base($"Nao ha recurso orbital disponivel para '{tipoSolicitado}'.")
    {
        TipoSolicitado = tipoSolicitado;
    }

    public TipoIntervencao TipoSolicitado { get; }
}
