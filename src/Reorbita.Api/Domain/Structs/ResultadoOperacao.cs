namespace Reorbita.Api.Domain.Structs;

public sealed class ResultadoOperacao<T>
{
    private ResultadoOperacao(bool sucesso, T? dados, string mensagem, int codigoHttp, string? codigoErro)
    {
        Sucesso = sucesso;
        Dados = dados;
        Mensagem = mensagem;
        CodigoHttp = codigoHttp;
        CodigoErro = codigoErro;
    }

    public bool Sucesso { get; }

    public T? Dados { get; }

    public string Mensagem { get; }

    public int CodigoHttp { get; }

    public string? CodigoErro { get; }

    public static ResultadoOperacao<T> Ok(T dados, string mensagem = "Operacao concluida.", int codigoHttp = 200)
    {
        return new ResultadoOperacao<T>(true, dados, mensagem, codigoHttp, null);
    }

    public static ResultadoOperacao<T> Falha(string mensagem, int codigoHttp, string codigoErro)
    {
        return new ResultadoOperacao<T>(false, default, mensagem, codigoHttp, codigoErro);
    }
}
