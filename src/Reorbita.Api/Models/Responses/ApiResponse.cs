namespace Reorbita.Api.Models.Responses;

public sealed class ApiResponse<T>
{
    public required bool Sucesso { get; init; }

    public required string Mensagem { get; init; }

    public string? CodigoErro { get; init; }

    public T? Dados { get; init; }
}
