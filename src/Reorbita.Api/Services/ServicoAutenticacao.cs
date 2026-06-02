using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;
using Reorbita.Api.Infrastructure;

namespace Reorbita.Api.Services;

public sealed class ServicoAutenticacao : IServicoAutenticacao
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IHashCredencial _hashCredencial;
    private readonly ILogger<ServicoAutenticacao> _logger;

    public ServicoAutenticacao(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IHashCredencial hashCredencial,
        ILogger<ServicoAutenticacao> logger)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _hashCredencial = hashCredencial;
        _logger = logger;
    }

    public ResultadoOperacao<TokenAcesso> GerarToken(SolicitacaoTokenAcesso solicitacaoTokenAcesso)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(solicitacaoTokenAcesso.UsuarioId))
            {
                return ResultadoOperacao<TokenAcesso>.Falha("UsuarioId e obrigatorio para emissao do token.", 400, "USUARIO_ID_OBRIGATORIO");
            }

            if (string.IsNullOrWhiteSpace(solicitacaoTokenAcesso.Operadora))
            {
                return ResultadoOperacao<TokenAcesso>.Falha("Operadora e obrigatoria para emissao do token.", 400, "OPERADORA_OBRIGATORIA");
            }

            var hashCodigoAcesso = Environment.GetEnvironmentVariable("REORBITA_DEV_AUTH_CODE_HASH_BCRYPT");
            if (!string.IsNullOrWhiteSpace(hashCodigoAcesso))
            {
                if (string.IsNullOrWhiteSpace(solicitacaoTokenAcesso.CodigoAcesso))
                {
                    return ResultadoOperacao<TokenAcesso>.Falha("Codigo de acesso obrigatorio para emissao do token.", 401, "CODIGO_ACESSO_OBRIGATORIO");
                }

                var codigoAcessoValido = _hashCredencial.VerificarHash(solicitacaoTokenAcesso.CodigoAcesso, hashCodigoAcesso);
                if (!codigoAcessoValido)
                {
                    return ResultadoOperacao<TokenAcesso>.Falha("Codigo de acesso invalido.", 401, "CODIGO_ACESSO_INVALIDO");
                }
            }
            else if (!_hostEnvironment.IsDevelopment())
            {
                return ResultadoOperacao<TokenAcesso>.Falha(
                    "Emissao de token de desenvolvimento bloqueada fora do ambiente Development.",
                    403,
                    "EMISSAO_TOKEN_BLOQUEADA");
            }

            var issuer = _configuration["Seguranca:JwtIssuer"] ?? "Reorbita.Api";
            var audience = _configuration["Seguranca:JwtAudience"] ?? "Reorbita.Operadoras";
            var expiracaoMinutos = Math.Clamp(_configuration.GetValue<int>("Seguranca:JwtExpiracaoMinutos", 120), 5, 720);

            var instanteEmissaoUtc = DateTime.UtcNow;
            var instanteExpiracaoUtc = instanteEmissaoUtc.AddMinutes(expiracaoMinutos);
            var tokenId = Guid.NewGuid().ToString("N");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, solicitacaoTokenAcesso.UsuarioId),
                new(ClaimTypes.NameIdentifier, solicitacaoTokenAcesso.UsuarioId),
                new(ClaimTypes.Name, solicitacaoTokenAcesso.UsuarioId),
                new(ClaimTypes.Role, solicitacaoTokenAcesso.NivelAcesso.ToString()),
                new("operadora", solicitacaoTokenAcesso.Operadora),
                new("mfa", solicitacaoTokenAcesso.MfaHabilitado ? "true" : "false"),
                new(JwtRegisteredClaimNames.Jti, tokenId)
            };

            var chaveSeguranca = new SymmetricSecurityKey(JwtChaveProvider.ObterChave());
            var credenciaisAssinatura = new SigningCredentials(chaveSeguranca, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: instanteEmissaoUtc,
                expires: instanteExpiracaoUtc,
                signingCredentials: credenciaisAssinatura);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            var tokenAcesso = new TokenAcesso(
                accessToken,
                "Bearer",
                instanteExpiracaoUtc,
                (int)(instanteExpiracaoUtc - instanteEmissaoUtc).TotalSeconds);

            _logger.LogInformation(
                "Token JWT emitido. Usuario={UsuarioId}, Operadora={Operadora}, NivelAcesso={NivelAcesso}",
                solicitacaoTokenAcesso.UsuarioId,
                solicitacaoTokenAcesso.Operadora,
                solicitacaoTokenAcesso.NivelAcesso);

            return ResultadoOperacao<TokenAcesso>.Ok(tokenAcesso, "Token gerado com sucesso.", 200);
        }
        catch (Exception exception)
        {
            return TratarFalhaGeracaoToken(exception);
        }
    }

    private ResultadoOperacao<TokenAcesso> TratarFalhaGeracaoToken(Exception exception)
    {
        switch (exception)
        {
            case SateliteNaoEncontradoException sateliteException:
                _logger.LogError(sateliteException, "Erro inesperado de dominio durante emissao de token. Satelite={SateliteId}", sateliteException.SateliteId);
                return ResultadoOperacao<TokenAcesso>.Falha("Erro de dominio inesperado ao gerar token.", 500, "ERRO_DOMINIO");
            case TelemetriaInvalidaException telemetriaException:
                _logger.LogWarning(telemetriaException, "Telemetria invalida inesperada durante emissao de token. Motivo={Motivo}", telemetriaException.Motivo);
                return ResultadoOperacao<TokenAcesso>.Falha("Erro de validacao inesperado ao gerar token.", 500, "ERRO_VALIDACAO");
            case IntervencaoNaoAutorizadaException intervencaoException:
                _logger.LogCritical(intervencaoException, "Intervencao nao autorizada inesperada durante emissao de token. Satelite={SateliteId}, Robo={RoboId}", intervencaoException.SateliteId, intervencaoException.RoboId);
                return ResultadoOperacao<TokenAcesso>.Falha("Erro de autorizacao inesperado ao gerar token.", 500, "ERRO_AUTORIZACAO");
            case RecursoOrbitalIndisponivelException recursoException:
                _logger.LogWarning(recursoException, "Recurso orbital indisponivel inesperado durante emissao de token. Tipo={TipoSolicitado}", recursoException.TipoSolicitado);
                return ResultadoOperacao<TokenAcesso>.Falha("Erro operacional inesperado ao gerar token.", 500, "ERRO_OPERACIONAL");
            case FalhaDeComunicacaoOrbitalException falhaComunicacaoException:
                _logger.LogError(falhaComunicacaoException, "Falha de comunicacao orbital inesperada durante emissao de token.");
                return ResultadoOperacao<TokenAcesso>.Falha("Erro de comunicacao inesperado ao gerar token.", 500, "ERRO_COMUNICACAO");
            case IntegridadeDadosComprometidaException integridadeException:
                _logger.LogCritical(integridadeException, "Integridade comprometida inesperada durante emissao de token. Arquivo={Arquivo}", integridadeException.CaminhoArquivo);
                return ResultadoOperacao<TokenAcesso>.Falha("Erro de integridade inesperado ao gerar token.", 500, "ERRO_INTEGRIDADE");
            default:
                _logger.LogError(exception, "Erro inesperado ao gerar token JWT.");
                return ResultadoOperacao<TokenAcesso>.Falha("Erro interno ao gerar token JWT.", 500, "ERRO_INTERNO");
        }
    }
}
