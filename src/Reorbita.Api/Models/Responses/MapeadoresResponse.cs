using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Models.Requests;

namespace Reorbita.Api.Models.Responses;

public static class MapeadoresResponse
{
    public static SateliteResponse ParaResponse(this Satelite satelite)
    {
        return satelite switch
        {
            SateliteComercial comercial => new SateliteResponse
            {
                Id = comercial.Id,
                TipoSatelite = nameof(SateliteComercial),
                Nome = comercial.Nome,
                Operadora = comercial.Operadora,
                OrbitaAtual = comercial.OrbitaAtual,
                DataLancamento = comercial.DataLancamento,
                StatusAtual = comercial.StatusAtual,
                NivelCombustivel = comercial.NivelCombustivel,
                DegradacaoBateria = comercial.DegradacaoBateria,
                SlaContratado = comercial.SlaContratado
            },
            SateliteCientifico cientifico => new SateliteResponse
            {
                Id = cientifico.Id,
                TipoSatelite = nameof(SateliteCientifico),
                Nome = cientifico.Nome,
                Operadora = cientifico.Operadora,
                OrbitaAtual = cientifico.OrbitaAtual,
                DataLancamento = cientifico.DataLancamento,
                StatusAtual = cientifico.StatusAtual,
                NivelCombustivel = cientifico.NivelCombustivel,
                DegradacaoBateria = cientifico.DegradacaoBateria,
                MissaoAtiva = cientifico.MissaoAtiva
            },
            SateliteDefesa defesa => new SateliteResponse
            {
                Id = defesa.Id,
                TipoSatelite = nameof(SateliteDefesa),
                Nome = defesa.Nome,
                Operadora = defesa.Operadora,
                OrbitaAtual = defesa.OrbitaAtual,
                DataLancamento = defesa.DataLancamento,
                StatusAtual = defesa.StatusAtual,
                NivelCombustivel = defesa.NivelCombustivel,
                DegradacaoBateria = defesa.DegradacaoBateria
            },
            _ => throw new InvalidOperationException($"Tipo de satelite nao suportado: {satelite.GetType().Name}")
        };
    }

    public static RelatorioSaudeResponse ParaResponse(this RelatorioSaude relatorioSaude)
    {
        return new RelatorioSaudeResponse
        {
            SateliteId = relatorioSaude.SateliteId,
            NomeSatelite = relatorioSaude.NomeSatelite,
            StatusAtual = relatorioSaude.StatusAtual,
            DataHoraAtualizacao = relatorioSaude.DataHoraAtualizacao,
            NivelCombustivel = relatorioSaude.NivelCombustivel,
            DegradacaoBateria = relatorioSaude.DegradacaoBateria,
            AlertasAtivos = relatorioSaude.AlertasAtivos.Select(alerta => alerta.ParaResponse()).ToList().AsReadOnly()
        };
    }

    public static AlertaResponse ParaResponse(this Alerta alerta)
    {
        return new AlertaResponse
        {
            SateliteId = alerta.SateliteId,
            DataHoraCriacao = alerta.DataHoraCriacao,
            TipoAlerta = alerta.TipoAlerta,
            Descricao = alerta.Descricao,
            AcaoRecomendada = alerta.DefinirAcaoRecomendada()
        };
    }

    public static OrdemServicoResponse ParaResponse(this OrdemServico ordemServico)
    {
        return new OrdemServicoResponse
        {
            Id = ordemServico.Id,
            SateliteId = ordemServico.SateliteId,
            RoboId = ordemServico.RoboId,
            TipoIntervencao = ordemServico.TipoIntervencao,
            DataHoraAgendada = ordemServico.DataHoraAgendada,
            StatusOrdem = ordemServico.StatusOrdem,
            DataHoraConclusao = ordemServico.DataHoraConclusao
        };
    }

    public static RoboOrbitalResponse ParaResponse(this RoboOrbital roboOrbital)
    {
        return new RoboOrbitalResponse
        {
            Id = roboOrbital.Id,
            TipoEspecializacao = roboOrbital.TipoEspecializacao,
            Disponivel = roboOrbital.Disponivel,
            EstacaoMaeId = roboOrbital.EstacaoMaeId
        };
    }

    public static Satelite ParaEntidade(this CriarSateliteRequest request)
    {
        return request.TipoSatelite.Trim().ToLowerInvariant() switch
        {
            "satelitecomercial" or "comercial" => new SateliteComercial(
                request.Id,
                request.Nome,
                request.Operadora,
                request.OrbitaAtual,
                request.DataLancamentoUtc,
                request.StatusInicial,
                request.NivelCombustivelInicial,
                request.DegradacaoBateriaInicial,
                request.SlaContratado ?? 3),
            "satelitecientifico" or "cientifico" => new SateliteCientifico(
                request.Id,
                request.Nome,
                request.Operadora,
                request.OrbitaAtual,
                request.DataLancamentoUtc,
                request.StatusInicial,
                request.NivelCombustivelInicial,
                request.DegradacaoBateriaInicial,
                request.MissaoAtiva ?? true),
            "satelitedefesa" or "defesa" => new SateliteDefesa(
                request.Id,
                request.Nome,
                request.Operadora,
                request.OrbitaAtual,
                request.DataLancamentoUtc,
                request.StatusInicial,
                request.NivelCombustivelInicial,
                request.DegradacaoBateriaInicial),
            _ => throw new InvalidOperationException("Tipo de satelite invalido para cadastro.")
        };
    }

    public static Satelite ParaEntidadeAtualizada(this AtualizarSateliteRequest request, Satelite sateliteBase)
    {
        return sateliteBase switch
        {
            SateliteComercial comercial => new SateliteComercial(
                comercial.Id,
                request.Nome,
                comercial.Operadora,
                request.OrbitaAtual,
                comercial.DataLancamento,
                request.StatusAtual,
                request.NivelCombustivel,
                request.DegradacaoBateria,
                request.SlaContratado ?? comercial.SlaContratado),
            SateliteCientifico cientifico => new SateliteCientifico(
                cientifico.Id,
                request.Nome,
                cientifico.Operadora,
                request.OrbitaAtual,
                cientifico.DataLancamento,
                request.StatusAtual,
                request.NivelCombustivel,
                request.DegradacaoBateria,
                request.MissaoAtiva ?? cientifico.MissaoAtiva),
            SateliteDefesa defesa => new SateliteDefesa(
                defesa.Id,
                request.Nome,
                defesa.Operadora,
                request.OrbitaAtual,
                defesa.DataLancamento,
                request.StatusAtual,
                request.NivelCombustivel,
                request.DegradacaoBateria),
            _ => throw new InvalidOperationException("Tipo de satelite invalido para atualizacao.")
        };
    }
}
