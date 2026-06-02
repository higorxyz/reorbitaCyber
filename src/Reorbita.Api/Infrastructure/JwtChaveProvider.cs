using System.Security.Cryptography;
using System.Text;

namespace Reorbita.Api.Infrastructure;

public static class JwtChaveProvider
{
    public static byte[] ObterChave()
    {
        var chaveBase64 = Environment.GetEnvironmentVariable("REORBITA_JWT_KEY_BASE64");
        if (!string.IsNullOrWhiteSpace(chaveBase64))
        {
            var chaveBytes = Convert.FromBase64String(chaveBase64);
            if (chaveBytes.Length >= 32)
            {
                return chaveBytes[..32];
            }
        }

        // Deriva chave local quando a variavel de ambiente nao estiver configurada.
        var material = $"{Environment.MachineName}:{Environment.UserName}:REORBITA-JWT";
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(material));
    }
}
