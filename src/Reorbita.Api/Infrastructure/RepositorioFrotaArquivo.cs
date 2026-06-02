using System.Text.Json;
using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Interfaces;

namespace Reorbita.Api.Infrastructure;

public sealed class RepositorioFrotaArquivo : IRepositorioFrota
{
    private readonly ILogger<RepositorioFrotaArquivo> _logger;
    private readonly string _arquivoFrota;
    private readonly string _arquivoHash;
    private readonly object _sync = new();

    public RepositorioFrotaArquivo(ILogger<RepositorioFrotaArquivo> logger, IHostEnvironment hostEnvironment)
    {
        _logger = logger;

        var pastaDados = Path.Combine(hostEnvironment.ContentRootPath, "Infrastructure", "Data");
        Directory.CreateDirectory(pastaDados);

        _arquivoFrota = Path.Combine(pastaDados, "frota.json");
        _arquivoHash = Path.Combine(pastaDados, "frota.hash");

        if (!File.Exists(_arquivoFrota))
        {
            var repositorioMemoria = new RepositorioFrotaMemoria();
            Persistir(repositorioMemoria.ListarTodos().ToList());
        }
    }

    public RoboOrbital? ObterDisponivelPara(TipoIntervencao tipoIntervencao)
    {
        lock (_sync)
        {
            var robos = CarregarTodosInterno();
            return robos.FirstOrDefault(robo => robo.Disponivel && robo.VerificarCompatibilidade(tipoIntervencao));
        }
    }

    public IReadOnlyCollection<RoboOrbital> ListarTodos()
    {
        lock (_sync)
        {
            return CarregarTodosInterno().AsReadOnly();
        }
    }

    public void Atualizar(RoboOrbital roboOrbital)
    {
        lock (_sync)
        {
            var robos = CarregarTodosInterno();
            robos.RemoveAll(item => item.Id.Equals(roboOrbital.Id, StringComparison.OrdinalIgnoreCase));
            robos.Add(roboOrbital);
            Persistir(robos);
        }
    }

    private List<RoboOrbital> CarregarTodosInterno()
    {
        VerificarIntegridadeArquivoExistente();

        var payloadCriptografado = File.ReadAllText(_arquivoFrota);
        if (string.IsNullOrWhiteSpace(payloadCriptografado))
        {
            return [];
        }

        var json = CriptografiaArquivoHelper.Descriptografar(payloadCriptografado);
        var robosPersistidos = JsonSerializer.Deserialize<List<RoboPersistido>>(json) ?? [];

        return robosPersistidos.Select(MapearParaEntidade).ToList();
    }

    private void Persistir(IReadOnlyCollection<RoboOrbital> robos)
    {
        VerificarIntegridadeArquivoExistente();

        var robosPersistidos = robos.Select(MapearParaPersistencia).ToList();
        var json = JsonSerializer.Serialize(robosPersistidos, new JsonSerializerOptions { WriteIndented = true });
        var payloadCriptografado = CriptografiaArquivoHelper.Criptografar(json);
        var hash = IntegridadeArquivoHelper.GerarHashSha256(payloadCriptografado);

        File.WriteAllText(_arquivoFrota, payloadCriptografado);
        File.WriteAllText(_arquivoHash, hash);

        VerificarIntegridadeArquivoExistente();
    }

    private void VerificarIntegridadeArquivoExistente()
    {
        if (!File.Exists(_arquivoFrota) || !File.Exists(_arquivoHash))
        {
            return;
        }

        var payloadCriptografado = File.ReadAllText(_arquivoFrota);
        var hashEsperado = File.ReadAllText(_arquivoHash).Trim();
        var hashEncontrado = IntegridadeArquivoHelper.GerarHashSha256(payloadCriptografado);

        if (!string.Equals(hashEsperado, hashEncontrado, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogCritical("Integridade de dados comprometida para {Arquivo}", _arquivoFrota);
            throw new IntegridadeDadosComprometidaException(_arquivoFrota, hashEsperado, hashEncontrado);
        }
    }

    private static RoboPersistido MapearParaPersistencia(RoboOrbital roboOrbital)
    {
        return roboOrbital switch
        {
            RoboReabastecedor reabastecedor => new RoboPersistido
            {
                Id = reabastecedor.Id,
                TipoRobo = nameof(RoboReabastecedor),
                Disponivel = reabastecedor.Disponivel,
                EstacaoMaeId = reabastecedor.EstacaoMaeId
            },
            RoboReparador reparador => new RoboPersistido
            {
                Id = reparador.Id,
                TipoRobo = nameof(RoboReparador),
                Disponivel = reparador.Disponivel,
                EstacaoMaeId = reparador.EstacaoMaeId
            },
            RoboCapturadorDetritos capturador => new RoboPersistido
            {
                Id = capturador.Id,
                TipoRobo = nameof(RoboCapturadorDetritos),
                Disponivel = capturador.Disponivel,
                EstacaoMaeId = capturador.EstacaoMaeId
            },
            _ => throw new InvalidOperationException($"Tipo de robo nao suportado: {roboOrbital.GetType().Name}")
        };
    }

    private static RoboOrbital MapearParaEntidade(RoboPersistido roboPersistido)
    {
        return roboPersistido.TipoRobo switch
        {
            nameof(RoboReabastecedor) => new RoboReabastecedor(roboPersistido.Id, roboPersistido.Disponivel, roboPersistido.EstacaoMaeId),
            nameof(RoboReparador) => new RoboReparador(roboPersistido.Id, roboPersistido.Disponivel, roboPersistido.EstacaoMaeId),
            nameof(RoboCapturadorDetritos) => new RoboCapturadorDetritos(roboPersistido.Id, roboPersistido.Disponivel, roboPersistido.EstacaoMaeId),
            _ => throw new InvalidOperationException($"Tipo de robo persistido desconhecido: {roboPersistido.TipoRobo}")
        };
    }

    private sealed class RoboPersistido
    {
        public required string Id { get; init; }

        public required string TipoRobo { get; init; }

        public required bool Disponivel { get; init; }

        public required string EstacaoMaeId { get; init; }
    }
}
