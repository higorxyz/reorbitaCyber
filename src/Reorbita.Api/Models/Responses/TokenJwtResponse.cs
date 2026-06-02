namespace Reorbita.Api.Models.Responses;

public sealed class TokenJwtResponse
{
    public required string AccessToken { get; init; }

    public required string TokenType { get; init; }

    public required DateTime ExpiraEmUtc { get; init; }

    public required int ExpiresInSeconds { get; init; }
}
