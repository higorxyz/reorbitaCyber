namespace Reorbita.Api.Models.Requests;

public sealed class RevogarTokenRequest
{
    public required string TokenId { get; init; }
}
