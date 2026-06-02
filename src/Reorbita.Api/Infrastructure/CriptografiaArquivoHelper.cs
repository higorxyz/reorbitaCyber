using System.Security.Cryptography;
using System.Text;

namespace Reorbita.Api.Infrastructure;

public static class CriptografiaArquivoHelper
{
    private static readonly byte[] _chaveAes = CarregarChaveAes();

    public static string Criptografar(string textoPlano)
    {
        var bytesTextoPlano = Encoding.UTF8.GetBytes(textoPlano);

        using var aes = Aes.Create();
        aes.Key = _chaveAes;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var bytesCriptografados = encryptor.TransformFinalBlock(bytesTextoPlano, 0, bytesTextoPlano.Length);

        var payload = new byte[aes.IV.Length + bytesCriptografados.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
        Buffer.BlockCopy(bytesCriptografados, 0, payload, aes.IV.Length, bytesCriptografados.Length);

        return Convert.ToBase64String(payload);
    }

    public static string Descriptografar(string conteudoCriptografado)
    {
        var payload = Convert.FromBase64String(conteudoCriptografado);
        var iv = payload[..16];
        var bytesCriptografados = payload[16..];

        using var aes = Aes.Create();
        aes.Key = _chaveAes;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var bytesTextoPlano = decryptor.TransformFinalBlock(bytesCriptografados, 0, bytesCriptografados.Length);

        return Encoding.UTF8.GetString(bytesTextoPlano);
    }

    private static byte[] CarregarChaveAes()
    {
        var chaveBase64Ambiente = Environment.GetEnvironmentVariable("REORBITA_DATA_KEY_BASE64");
        if (!string.IsNullOrWhiteSpace(chaveBase64Ambiente))
        {
            var bytesChave = Convert.FromBase64String(chaveBase64Ambiente);
            if (bytesChave.Length == 32)
            {
                return bytesChave;
            }
        }

        // Deriva chave local quando a chave de ambiente nao estiver disponivel.
        var material = $"{Environment.MachineName}:{Environment.UserName}:REORBITA";
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(material));
    }
}
