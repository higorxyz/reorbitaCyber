using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Entities;

public sealed class OrdemServico
{
    public required string Id { get; init; }

    public required string SateliteId { get; init; }

    public required string RoboId { get; init; }

    public required TipoIntervencao TipoIntervencao { get; init; }

    public required DateTime DataHoraAgendada { get; init; }

    public string StatusOrdem { get; private set; } = "Agendada";

    public DateTime? DataHoraConclusao { get; private set; }

    public void MarcarComoConcluida()
    {
        StatusOrdem = "Concluida";
        DataHoraConclusao = DateTime.UtcNow;
    }
}
