using DryIoc;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Data.Sessions;
using OrionIrcd.Server.Interfaces.Services;
using Serilog;

namespace OrionIrcd.Server.Services.Listeners;

public sealed class IrcCommandDispatcherService : IIrcCommandDispatcherService
{
    private readonly IContainer _container;
    private readonly ILogger _logger = Log.ForContext<IrcCommandDispatcherService>();
    private readonly List<IrcCommandDispatchRegistration> _registrations;

    public IrcCommandDispatcherService(List<IrcCommandDispatchRegistration> registrations, IContainer container)
    {
        _registrations = registrations;
        _container = container;
    }

    public async Task DispatchAsync(
        NetworkSession session,
        IIrcCommand command,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var invocations = _registrations
                          .Where(registration => registration.CanDispatch(command))
                          .SelectMany(
                              registration => registration.CreateInvocations(
                                  _container,
                                  session,
                                  command
                              )
                          )
                          .ToArray();
        var tasks = invocations.Select(
            invocation => DispatchListenerAsync(
                invocation,
                session,
                cancellationToken
            )
        );

        await Task.WhenAll(tasks);
    }

    private async Task DispatchListenerAsync(
        IrcCommandDispatchInvocation invocation,
        NetworkSession session,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await invocation.HandleAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.Error(
                exception,
                "IRC command listener {ListenerType} failed while handling command {CommandType}",
                invocation.Listener.GetType().FullName,
                invocation.CommandType.FullName
            );
        }
    }
}
