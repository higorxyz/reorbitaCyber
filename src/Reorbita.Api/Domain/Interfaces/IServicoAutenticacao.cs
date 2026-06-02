using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Interfaces;

public interface IServicoAutenticacao
{
    ResultadoOperacao<TokenAcesso> GerarToken(SolicitacaoTokenAcesso solicitacaoTokenAcesso);
}
