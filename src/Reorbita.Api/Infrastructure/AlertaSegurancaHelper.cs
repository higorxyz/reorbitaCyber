namespace Reorbita.Api.Infrastructure;

public static class AlertaSegurancaHelper
{
    private static readonly EventId _eventIdEscaladaPrivilegio = new(9001, "EscaladaPrivilegioDetectada");

    public static void RegistrarEscaladaPrivilegio(ILogger logger, string sateliteId, string roboId)
    {
        logger.LogCritical(
            _eventIdEscaladaPrivilegio,
            "ALERTA_SEGURANCA_AUTOMATICO: tentativa de intervencao nao autorizada. Satelite={SateliteId}, Robo={RoboId}",
            sateliteId,
            roboId);
    }
}
