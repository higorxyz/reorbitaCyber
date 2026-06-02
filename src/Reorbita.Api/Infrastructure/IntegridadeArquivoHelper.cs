using System.Security.Cryptography;
using System.Text;

namespace Reorbita.Api.Infrastructure;

public static class IntegridadeArquivoHelper
{
    public static string GerarHashSha256(string conteudo)
    {
        var bytesConteudo = Encoding.UTF8.GetBytes(conteudo);
        return GerarHashSha256(bytesConteudo);
    }

    public static string GerarHashSha256(byte[] bytesConteudo)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytesConteudo);
        return Convert.ToHexString(hash);
    }

    public static bool HashConfere(string conteudo, string hashEsperado)
    {
        var hashEncontrado = GerarHashSha256(conteudo);
        return string.Equals(hashEncontrado, hashEsperado, StringComparison.OrdinalIgnoreCase);
    }

    public static bool HashConfere(byte[] bytesConteudo, string hashEsperado)
    {
        var hashEncontrado = GerarHashSha256(bytesConteudo);
        return string.Equals(hashEncontrado, hashEsperado, StringComparison.OrdinalIgnoreCase);
    }
}
