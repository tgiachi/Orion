namespace OrionIrcd.Server.Interfaces.Services;

public interface IOrionIrcdOrchestrator
{
    Task RunAsync(CancellationToken cancellationToken);
}
