using Reorbita.Api.Domain.Interfaces;

namespace Reorbita.Api.Infrastructure;

public sealed class BcryptHashCredencial : IHashCredencial
{
    public string GerarHash(string senhaTextoPlano)
    {
        return BCrypt.Net.BCrypt.HashPassword(senhaTextoPlano, workFactor: 12);
    }

    public bool VerificarHash(string senhaTextoPlano, string hashArmazenado)
    {
        return BCrypt.Net.BCrypt.Verify(senhaTextoPlano, hashArmazenado);
    }
}
