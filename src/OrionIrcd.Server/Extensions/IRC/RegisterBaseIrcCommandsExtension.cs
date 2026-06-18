using DryIoc;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;
using OrionIrcd.IRC.Services;
using OrionIrcd.Server.Data.Events;
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
            container.Register<IIrcReplyService, IrcReplyService>(Reuse.Singleton);

            var stateService = new IrcSessionStateService();
            container.RegisterInstance<IIrcSessionStateService>(stateService);
            container.RegisterInstance<ISyncEventListener<NetworkSessionDisconnectedEvent>>(stateService);

            container.Register<IAsyncEventListener<NetworkResultReceivedEvent<string>>, IrcCommandPipelineService>(
                Reuse.Singleton
            );

            container.RegisterIrcCommandList<CapCommand, CapCommandListener>();
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
        registry.RegisterCommand<CapCommand>(BindCap);
        registry.RegisterCommand<NickCommand>(BindNick);
        registry.RegisterCommand<PassCommand>(BindPass);
        registry.RegisterCommand<PingCommand>(BindPing);
        registry.RegisterCommand<PongCommand>(BindPong);
        registry.RegisterCommand<QuitCommand>(BindQuit);
        registry.RegisterCommand<UserCommand>(BindUser);
    }

    private static void BindCap(CapCommand command, RawIrcMessage raw)
    {
        command.Subcommand = raw.Params.Count > 0 ? raw.Params[0] : string.Empty;
        command.Capabilities = raw.Params.Skip(1).ToArray();
    }

    private static void BindNick(NickCommand command, RawIrcMessage raw)
    {
        command.Nickname = raw.Params.Count > 0 ? raw.Params[0] : string.Empty;
    }

    private static void BindPass(PassCommand command, RawIrcMessage raw)
    {
        command.Password = raw.Trailing ?? (raw.Params.Count > 0 ? raw.Params[0] : string.Empty);
    }

    private static void BindPing(PingCommand command, RawIrcMessage raw)
    {
        command.Token = raw.Trailing ?? (raw.Params.Count > 0 ? raw.Params[0] : string.Empty);
    }

    private static void BindPong(PongCommand command, RawIrcMessage raw)
    {
        command.Token = raw.Trailing ?? (raw.Params.Count > 0 ? raw.Params[0] : string.Empty);
    }

    private static void BindQuit(QuitCommand command, RawIrcMessage raw)
    {
        command.Reason = raw.Trailing ?? string.Empty;
    }

    private static void BindUser(UserCommand command, RawIrcMessage raw)
    {
        command.Username = raw.Params.Count > 0 ? raw.Params[0] : string.Empty;
        command.Mode = raw.Params.Count > 1 ? raw.Params[1] : string.Empty;
        command.Unused = raw.Params.Count > 2 ? raw.Params[2] : string.Empty;
        command.RealName = raw.Trailing ?? string.Empty;
    }
}
