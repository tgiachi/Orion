using DryIoc;
using OrionIrcd.Core.Container;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;

namespace OrionIrcd.Server.Extensions.Listeners;

public static class RegisterIrcCommandListExtension
{
    extension(IContainer container)
    {
        public IContainer RegisterIrcCommandList<TCommand, TListener>()
            where TCommand : IIrcCommand
            where TListener : IIrcCommandListener<TCommand>
        {
            container.Register<IIrcCommandListener<TCommand>, TListener>(
                Reuse.Singleton,
                serviceKey: typeof(TListener)
            );

            container.AddToRegisterTypedList(
                IrcCommandDispatchRegistration.Create<TCommand, TListener>()
            );

            return container;
        }
    }
}
