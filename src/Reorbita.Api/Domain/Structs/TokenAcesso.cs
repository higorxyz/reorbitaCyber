namespace Reorbita.Api.Domain.Structs;

public readonly record struct TokenAcesso(
    string AccessToken,
    string TokenType,
    DateTime ExpiraEmUtc,
    int ExpiresInSeconds
);
