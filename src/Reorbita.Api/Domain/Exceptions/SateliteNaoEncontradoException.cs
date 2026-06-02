namespace Reorbita.Api.Domain.Exceptions;

public sealed class SateliteNaoEncontradoException : Exception
{
    public SateliteNaoEncontradoException(string sateliteId)
        : base($"Satelite com ID '{sateliteId}' nao foi encontrado.")
    {
        SateliteId = sateliteId;
    }

    public string SateliteId { get; }
}
