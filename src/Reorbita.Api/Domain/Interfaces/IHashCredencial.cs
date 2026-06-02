namespace Reorbita.Api.Domain.Interfaces;

public interface IHashCredencial
{
    string GerarHash(string senhaTextoPlano);

    bool VerificarHash(string senhaTextoPlano, string hashArmazenado);
}
