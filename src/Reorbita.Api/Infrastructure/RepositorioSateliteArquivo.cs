using System.Text.Json;
using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Enums;
using Reorbita.Api.Domain.Exceptions;
using Reorbita.Api.Domain.Interfaces;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Infrastructure;

public sealed class RepositorioSateliteArquivo : IRepositorioSatelite
{
    private readonly ILogger<RepositorioSateliteArquivo> _logger;
    private readonly string _arquivoSatelites;
    private readonly string _arquivoHash;
    private readonly object _sync = new();

    public RepositorioSateliteArquivo(ILogger<RepositorioSateliteArquivo> logger, IHostEnvironment hostEnvironment)
    {
        _logger = logger;

        var pastaDados = Path.Combine(hostEnvironment.ContentRootPath, "Infrastructure", "Data");
        Directory.CreateDirectory(pastaDados);

        _arquivoSatelites = Path.Combine(pastaDados, "satelites.json");
        _arquivoHash = Path.Combine(pastaDados, "satelites.hash");

        if (!File.Exists(_arquivoSatelites))
        {
            var repositorioMemoria = new RepositorioSateliteMemoria();
            Persistir(repositorioMemoria.ListarTodos().ToList());
        }
    }

    public Satelite? ObterPorId(string id)
    {
        lock (_sync)
        {
            var satelites = CarregarTodosInterno();
            return satelites.FirstOrDefault(satelite => satelite.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }

    public IReadOnlyCollection<Satelite> ListarTodos()
    {
        lock (_sync)
        {
            return CarregarTodosInterno().AsReadOnly();
        }
    }

    public void Salvar(Satelite satelite)
    {
        lock (_sync)
        {
            var satelites = CarregarTodosInterno();
            satelites.RemoveAll(item => item.Id.Equals(satelite.Id, StringComparison.OrdinalIgnoreCase));
            satelites.Add(satelite);
            Persistir(satelites);
        }
    }

    public void Atualizar(Satelite satelite)
    {
        lock (_sync)
        {
            var satelites = CarregarTodosInterno();
            satelites.RemoveAll(item => item.Id.Equals(satelite.Id, StringComparison.OrdinalIgnoreCase));
            satelites.Add(satelite);
            Persistir(satelites);
        }
    }

    public void Remover(string id)
    {
        lock (_sync)
        {
            var satelites = CarregarTodosInterno();
            satelites.RemoveAll(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            Persistir(satelites);
        }
    }

    private List<Satelite> CarregarTodosInterno()
    {
        VerificarIntegridadeArquivoExistente();

        var payloadCriptografado = File.ReadAllText(_arquivoSatelites);
        if (string.IsNullOrWhiteSpace(payloadCriptografado))
        {
            return [];
        }

        var json = CriptografiaArquivoHelper.Descriptografar(payloadCriptografado);
        var satelitesPersistidos = JsonSerializer.Deserialize<List<SatelitePersistido>>(json) ?? [];

        return satelitesPersistidos.Select(MapearParaEntidade).ToList();
    }

    private void Persistir(IReadOnlyCollection<Satelite> satelites)
    {
        VerificarIntegridadeArquivoExistente();

        var satelitesPersistidos = satelites.Select(MapearParaPersistencia).ToList();
        var json = JsonSerializer.Serialize(satelitesPersistidos, new JsonSerializerOptions { WriteIndented = true });
        var payloadCriptografado = CriptografiaArquivoHelper.Criptografar(json);
        var hash = IntegridadeArquivoHelper.GerarHashSha256(payloadCriptografado);

        File.WriteAllText(_arquivoSatelites, payloadCriptografado);
        File.WriteAllText(_arquivoHash, hash);

        VerificarIntegridadeArquivoExistente();
    }

    private void VerificarIntegridadeArquivoExistente()
    {
        if (!File.Exists(_arquivoSatelites) || !File.Exists(_arquivoHash))
        {
            return;
        }

        var payloadCriptografado = File.ReadAllText(_arquivoSatelites);
        var hashEsperado = File.ReadAllText(_arquivoHash).Trim();
        var hashEncontrado = IntegridadeArquivoHelper.GerarHashSha256(payloadCriptografado);

        if (!string.Equals(hashEsperado, hashEncontrado, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogCritical("Integridade de dados comprometida para {Arquivo}", _arquivoSatelites);
            throw new IntegridadeDadosComprometidaException(_arquivoSatelites, hashEsperado, hashEncontrado);
        }
    }

    private static SatelitePersistido MapearParaPersistencia(Satelite satelite)
    {
        return satelite switch
        {
            SateliteComercial comercial => new SatelitePersistido
            {
                Id = comercial.Id,
                Nome = comercial.Nome,
                Operadora = comercial.Operadora,
                OrbitaAtual = comercial.OrbitaAtual,
                DataLancamento = comercial.DataLancamento,
                StatusAtual = comercial.StatusAtual,
                NivelCombustivel = comercial.NivelCombustivel,
                DegradacaoBateria = comercial.DegradacaoBateria,
                TipoSatelite = nameof(SateliteComercial),
                SlaContratado = comercial.SlaContratado
            },
            SateliteCientifico cientifico => new SatelitePersistido
            {
                Id = cientifico.Id,
                Nome = cientifico.Nome,
                Operadora = cientifico.Operadora,
                OrbitaAtual = cientifico.OrbitaAtual,
                DataLancamento = cientifico.DataLancamento,
                StatusAtual = cientifico.StatusAtual,
                NivelCombustivel = cientifico.NivelCombustivel,
                DegradacaoBateria = cientifico.DegradacaoBateria,
                TipoSatelite = nameof(SateliteCientifico),
                MissaoAtiva = cientifico.MissaoAtiva
            },
            SateliteDefesa defesa => new SatelitePersistido
            {
                Id = defesa.Id,
                Nome = defesa.Nome,
                Operadora = defesa.Operadora,
                OrbitaAtual = defesa.OrbitaAtual,
                DataLancamento = defesa.DataLancamento,
                StatusAtual = defesa.StatusAtual,
                NivelCombustivel = defesa.NivelCombustivel,
                DegradacaoBateria = defesa.DegradacaoBateria,
                TipoSatelite = nameof(SateliteDefesa)
            },
            _ => throw new InvalidOperationException($"Tipo de satelite nao suportado: {satelite.GetType().Name}")
        };
    }

    private static Satelite MapearParaEntidade(SatelitePersistido satelitePersistido)
    {
        return satelitePersistido.TipoSatelite switch
        {
            nameof(SateliteComercial) => new SateliteComercial(
                satelitePersistido.Id,
                satelitePersistido.Nome,
                satelitePersistido.Operadora,
                satelitePersistido.OrbitaAtual,
                satelitePersistido.DataLancamento,
                satelitePersistido.StatusAtual,
                satelitePersistido.NivelCombustivel,
                satelitePersistido.DegradacaoBateria,
                satelitePersistido.SlaContratado ?? 3),
            nameof(SateliteCientifico) => new SateliteCientifico(
                satelitePersistido.Id,
                satelitePersistido.Nome,
                satelitePersistido.Operadora,
                satelitePersistido.OrbitaAtual,
                satelitePersistido.DataLancamento,
                satelitePersistido.StatusAtual,
                satelitePersistido.NivelCombustivel,
                satelitePersistido.DegradacaoBateria,
                satelitePersistido.MissaoAtiva ?? false),
            nameof(SateliteDefesa) => new SateliteDefesa(
                satelitePersistido.Id,
                satelitePersistido.Nome,
                satelitePersistido.Operadora,
                satelitePersistido.OrbitaAtual,
                satelitePersistido.DataLancamento,
                satelitePersistido.StatusAtual,
                satelitePersistido.NivelCombustivel,
                satelitePersistido.DegradacaoBateria),
            _ => throw new InvalidOperationException($"Tipo de satelite persistido desconhecido: {satelitePersistido.TipoSatelite}")
        };
    }

    private sealed class SatelitePersistido
    {
        public required string Id { get; init; }

        public required string Nome { get; init; }

        public required string Operadora { get; init; }

        public required CoordenadaOrbital OrbitaAtual { get; init; }

        public required DateTime DataLancamento { get; init; }

        public required StatusSatelite StatusAtual { get; init; }

        public required double NivelCombustivel { get; init; }

        public required double DegradacaoBateria { get; init; }

        public required string TipoSatelite { get; init; }

        public int? SlaContratado { get; init; }

        public bool? MissaoAtiva { get; init; }
    }
}
