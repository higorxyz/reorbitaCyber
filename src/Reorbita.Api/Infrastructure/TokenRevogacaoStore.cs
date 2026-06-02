using System.Collections.Concurrent;

namespace Reorbita.Api.Infrastructure;

public sealed class TokenRevogacaoStore
{
    private readonly ConcurrentDictionary<string, DateTime> _tokensRevogados = new(StringComparer.Ordinal);

    public void RevogarToken(string tokenId)
    {
        _tokensRevogados[tokenId] = DateTime.UtcNow;
    }

    public bool EstaRevogado(string tokenId)
    {
        return _tokensRevogados.ContainsKey(tokenId);
    }
}
