namespace OrionIrcd.Server.Core.Interfaces.Services;

public interface IOrionIrcdOrchestrator
{
    Task RunAsync(CancellationToken cancellationToken);
}
