namespace Reorbita.Api.Domain.Exceptions;

public sealed class IntervencaoNaoAutorizadaException : Exception
{
    public IntervencaoNaoAutorizadaException(string sateliteId, string roboId)
        : base($"Intervencao nao autorizada para o satelite '{sateliteId}' com robo '{roboId}'.")
    {
        SateliteId = sateliteId;
        RoboId = roboId;
    }

    public string SateliteId { get; }

    public string RoboId { get; }
}
