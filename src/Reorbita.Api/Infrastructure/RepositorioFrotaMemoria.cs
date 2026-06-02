using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Interfaces;

namespace Reorbita.Api.Infrastructure;

public sealed class RepositorioFrotaMemoria : IRepositorioFrota
{
    private readonly Dictionary<string, RoboOrbital> _robos = new(StringComparer.OrdinalIgnoreCase);

    public RepositorioFrotaMemoria()
    {
        SeedDadosIniciais();
    }

    public RoboOrbital? ObterDisponivelPara(TipoIntervencao tipoIntervencao)
    {
        return _robos.Values.FirstOrDefault(robo => robo.Disponivel && robo.VerificarCompatibilidade(tipoIntervencao));
    }

    public IReadOnlyCollection<RoboOrbital> ListarTodos()
    {
        return _robos.Values.ToList().AsReadOnly();
    }

    public void Atualizar(RoboOrbital roboOrbital)
    {
        _robos[roboOrbital.Id] = roboOrbital;
    }

    private void SeedDadosIniciais()
    {
        if (_robos.Count > 0)
        {
            return;
        }

        var roboReabastecedorDisponivel = new RoboReabastecedor("ROBO-REA-001", true, "ESTACAO-ALFA");
        var roboReabastecedorIndisponivel = new RoboReabastecedor("ROBO-REA-002", false, "ESTACAO-ALFA");
        var roboReparadorDisponivel = new RoboReparador("ROBO-REP-001", true, "ESTACAO-BETA");
        var roboCapturadorDisponivel = new RoboCapturadorDetritos("ROBO-CAP-001", true, "ESTACAO-GAMA");

        _robos[roboReabastecedorDisponivel.Id] = roboReabastecedorDisponivel;
        _robos[roboReabastecedorIndisponivel.Id] = roboReabastecedorIndisponivel;
        _robos[roboReparadorDisponivel.Id] = roboReparadorDisponivel;
        _robos[roboCapturadorDisponivel.Id] = roboCapturadorDisponivel;
    }
}
