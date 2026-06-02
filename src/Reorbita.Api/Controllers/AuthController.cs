using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Infrastructure;
using Reorbita.Api.Domain.Structs;
using Reorbita.Api.Models.Requests;
using Reorbita.Api.Models.Responses;

namespace Reorbita.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IServicoAutenticacao _servicoAutenticacao;
    private readonly TokenRevogacaoStore _tokenRevogacaoStore;

    public AuthController(IServicoAutenticacao servicoAutenticacao, TokenRevogacaoStore tokenRevogacaoStore)
    {
        _servicoAutenticacao = servicoAutenticacao;
        _tokenRevogacaoStore = tokenRevogacaoStore;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public IActionResult GerarTokenJwt([FromBody] SolicitarTokenJwtRequest request)
    {
        var solicitacaoTokenAcesso = new SolicitacaoTokenAcesso(
            request.UsuarioId,
            request.Operadora,
            request.NivelAcesso,
            request.MfaHabilitado,
            request.CodigoAcesso);

        var resultado = _servicoAutenticacao.GerarToken(solicitacaoTokenAcesso);
        if (!resultado.Sucesso)
        {
            return StatusCode(resultado.CodigoHttp, new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = resultado.Mensagem,
                CodigoErro = resultado.CodigoErro
            });
        }

        var tokenAcesso = resultado.Dados!;

        return StatusCode(resultado.CodigoHttp, new ApiResponse<TokenJwtResponse>
        {
            Sucesso = true,
            Mensagem = resultado.Mensagem,
            Dados = new TokenJwtResponse
            {
                AccessToken = tokenAcesso.AccessToken,
                TokenType = tokenAcesso.TokenType,
                ExpiraEmUtc = tokenAcesso.ExpiraEmUtc,
                ExpiresInSeconds = tokenAcesso.ExpiresInSeconds
            }
        });
    }

    [HttpPost("revogar-atual")]
    [Authorize]
    public IActionResult RevogarTokenAtual()
    {
        var tokenId = User.FindFirstValue(JwtRegisteredClaimNames.Jti)
                      ?? User.FindFirstValue("jti");

        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return BadRequest(new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = "Token sem identificador (jti) para revogacao.",
                CodigoErro = "TOKEN_ID_AUSENTE"
            });
        }

        _tokenRevogacaoStore.RevogarToken(tokenId);

        return Ok(new ApiResponse<object>
        {
            Sucesso = true,
            Mensagem = "Token atual revogado com sucesso."
        });
    }

    [HttpPost("revogar")]
    [Authorize(Policy = "AdminComMfa")]
    public IActionResult RevogarTokenPorId([FromBody] RevogarTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TokenId))
        {
            return BadRequest(new ApiResponse<object>
            {
                Sucesso = false,
                Mensagem = "TokenId e obrigatorio para revogacao administrativa.",
                CodigoErro = "TOKEN_ID_OBRIGATORIO"
            });
        }

        _tokenRevogacaoStore.RevogarToken(request.TokenId.Trim());

        return Ok(new ApiResponse<object>
        {
            Sucesso = true,
            Mensagem = "Token revogado com sucesso."
        });
    }
}
