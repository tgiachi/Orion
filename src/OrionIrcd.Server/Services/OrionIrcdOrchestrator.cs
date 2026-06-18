using DryIoc;
using OrionIrcd.Core.Data.Internal;
using OrionIrcd.Core.Interfaces.Services;
using OrionIrcd.Server.Interfaces.Services;
using Serilog;

namespace OrionIrcd.Server.Services;

public class OrionIrcdOrchestrator : IOrionIrcdOrchestrator
{
    private readonly List<ServiceRegistrationObject> _serviceRegistrationObjects;
    private readonly IContainer _container;
    private readonly ILogger _logger = Log.ForContext<OrionIrcdOrchestrator>();

    public OrionIrcdOrchestrator(List<ServiceRegistrationObject> serviceRegistrationObjects, IContainer container)
    {
        _serviceRegistrationObjects = serviceRegistrationObjects;
        _container = container;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await StartServicesAsync().ConfigureAwait(false);

        _logger.Information("All services started. OrionIRCd is now running.");

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        finally
        {
            await StopServicesAsync().ConfigureAwait(false);
        }
    }

    private IOrionIrcdService ResolveService(ServiceRegistrationObject serviceRegistrationObject)
    {
        var service = _container.Resolve(serviceRegistrationObject.ServiceType);

        return service as IOrionIrcdService ??
               throw new InvalidOperationException(
                   $"Registered service '{serviceRegistrationObject.ServiceType.FullName}' does not implement {nameof(IOrionIrcdService)}."
               );
    }

    private async Task StartServicesAsync()
    {
        foreach (var serviceRegistrationObject in _serviceRegistrationObjects.OrderBy(service => service.Priority))
        {
            var service = ResolveService(serviceRegistrationObject);

            _logger.Information("Starting service {ServiceType}", serviceRegistrationObject.ImplementationType.Name);

            await service.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task StopServicesAsync()
    {
        foreach (var serviceRegistrationObject in _serviceRegistrationObjects.OrderByDescending(service => service.Priority))
        {
            var service = ResolveService(serviceRegistrationObject);

            try
            {
                _logger.Information("Stopping service {ServiceType}", serviceRegistrationObject.ImplementationType.Name);

                await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(
                    exception,
                    "Service {ServiceType} failed while stopping",
                    serviceRegistrationObject.ImplementationType.FullName
                );
            }
        }
    }
}
