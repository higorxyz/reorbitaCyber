namespace Reorbita.Api.Domain.Exceptions;

public sealed class IntegridadeDadosComprometidaException : Exception
{
    public IntegridadeDadosComprometidaException(string caminhoArquivo, string hashEsperado, string hashEncontrado)
        : base($"Integridade de dados comprometida no arquivo '{caminhoArquivo}'.")
    {
        CaminhoArquivo = caminhoArquivo;
        HashEsperado = hashEsperado;
        HashEncontrado = hashEncontrado;
    }

    public string CaminhoArquivo { get; }

    public string HashEsperado { get; }

    public string HashEncontrado { get; }
}
