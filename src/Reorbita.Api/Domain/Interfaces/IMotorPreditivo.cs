using Reorbita.Api.Domain.Entities;
using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Interfaces;

public interface IMotorPreditivo
{
    IReadOnlyCollection<Alerta> AnalisarTelemetria(Satelite satelite, LeituraTelemetria leituraTelemetria);

    PrevisaoFalha ProjetarFalha(Satelite satelite);
}
