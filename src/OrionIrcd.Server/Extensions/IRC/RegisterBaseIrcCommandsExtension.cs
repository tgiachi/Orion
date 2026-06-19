using DryIoc;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Services;
using OrionIrcd.Server.Core.Data.Events;
using OrionIrcd.Server.Extensions.Listeners;
using OrionIrcd.Server.Interfaces.Services;
using OrionIrcd.Server.Services.IRC;
using OrionIrcd.Server.Services.IRC.Listeners;
using OrionIrcd.Server.Services.Listeners;

namespace OrionIrcd.Server.Extensions.IRC;

public static class RegisterBaseIrcCommandsExtension
{
    extension(IContainer container)
    {
        public IContainer RegisterBaseIrcCommands()
        {
            var registry = new IrcCommandRegistry();
            RegisterCommands(registry);

            container.RegisterInstance<IIrcCommandRegistry>(registry);
            container.Register<IrcCommandBinder>(Reuse.Singleton);
            container.Register<IIrcMessageParser, IrcMessageParser>(Reuse.Singleton);
            container.Register<IIrcCommandFactory, IrcCommandFactory>(Reuse.Singleton);
            container.Register<IIrcCommandDispatcherService, IrcCommandDispatcherService>(Reuse.Singleton);
            container.Register<IIrcMotdService, IrcMotdService>(Reuse.Singleton);
            container.Register<IIrcReplyService, IrcReplyService>(Reuse.Singleton);
            container.Register<IIrcRegistrationService, IrcRegistrationService>(Reuse.Singleton);

            var stateService = new IrcSessionStateService();
            container.RegisterInstance<IIrcSessionStateService>(stateService);
            container.RegisterInstance<ISyncEventListener<NetworkSessionDisconnectedEvent>>(stateService);

            container.Register<IAsyncEventListener<NetworkResultReceivedEvent<string>>, IrcCommandPipelineService>(
                Reuse.Singleton
            );

            container.RegisterIrcCommandList<CapCommand, CapCommandListener>();
            container.RegisterIrcCommandList<MotdCommand, MotdCommandListener>();
            container.RegisterIrcCommandList<NickCommand, NickCommandListener>();
            container.RegisterIrcCommandList<PassCommand, PassCommandListener>();
            container.RegisterIrcCommandList<PingCommand, PingCommandListener>();
            container.RegisterIrcCommandList<PongCommand, PongCommandListener>();
            container.RegisterIrcCommandList<QuitCommand, QuitCommandListener>();
            container.RegisterIrcCommandList<UserCommand, UserCommandListener>();

            return container;
        }
    }

    private static void RegisterCommands(IrcCommandRegistry registry)
    {
        registry.RegisterCommand<CapCommand>();
        registry.RegisterCommand<MotdCommand>();
        registry.RegisterCommand<NickCommand>();
        registry.RegisterCommand<PassCommand>();
        registry.RegisterCommand<PingCommand>();
        registry.RegisterCommand<PongCommand>();
        registry.RegisterCommand<QuitCommand>();
        registry.RegisterCommand<UserCommand>();
    }
}
