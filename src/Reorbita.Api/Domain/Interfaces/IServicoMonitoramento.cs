using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Interfaces;

public interface IServicoMonitoramento
{
    ResultadoOperacao<RelatorioSaude> ReceberTelemetria(string sateliteId, LeituraTelemetria leituraTelemetria);

    ResultadoOperacao<IReadOnlyCollection<LeituraTelemetria>> ObterHistoricoTelemetria(string sateliteId, DateTime? dataInicioUtc, DateTime? dataFimUtc);

    ResultadoOperacao<IReadOnlyCollection<Satelite>> ListarSatelitesPorOperadora(string operadora);

    ResultadoOperacao<Satelite> ObterSatelitePorId(string id, string operadora);

    ResultadoOperacao<Satelite> CadastrarSatelite(Satelite satelite);

    ResultadoOperacao<Satelite> AtualizarSatelite(string id, Satelite sateliteAtualizado, string operadora);
}
