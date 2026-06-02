namespace Reorbita.Api.Domain.Exceptions;

public sealed class FalhaDeComunicacaoOrbitalException : Exception
{
    public FalhaDeComunicacaoOrbitalException(string mensagem, Exception? innerException = null)
        : base(mensagem, innerException)
    {
    }
}
