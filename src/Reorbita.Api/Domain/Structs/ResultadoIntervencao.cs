using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Structs;

public readonly record struct ResultadoIntervencao(
    bool Sucesso,
    string Mensagem,
    DateTime DataHoraExecucao,
    TipoIntervencao TipoExecutado
);
