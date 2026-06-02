using Reorbita.Api.Domain.Enums;

namespace Reorbita.Api.Domain.Interfaces;

public interface IMonitoravel
{
    StatusSatelite AvaliarSaude();

    string ObterIdAtivo();

    StatusSatelite ObterStatusAtual();
}
