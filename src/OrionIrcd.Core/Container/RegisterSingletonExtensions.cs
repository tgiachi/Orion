using DryIoc;
using OrionIrcd.Core.Data.Internal;

namespace OrionIrcd.Core.Container;

/// <summary>
/// Provides extension methods for registering services as singletons in the DryIoc dependency injection container.
/// </summary>
public static class RegisterSingletonExtensions
{
    extension(IContainer container)
    {
        /// <summary>
        /// Registers a service with its implementation as a singleton in the container.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="autoStart">Whether to auto-start the service.</param>
        /// <returns>The container for method chaining.</returns>
        public IContainer RegisterService<TService, TImplementation>(int priority = 0)
            where TImplementation : TService
        {
            container.Register<TService, TImplementation>(
                Reuse.Singleton,
                setup: Setup.With(
                    allowDisposableTransient: true,
                    trackDisposableTransient: true
                )
            );

            container.AddToRegisterTypedList(new ServiceRegistrationObject(typeof(TService), typeof(TImplementation), priority));

            return container;
        }

        /// <summary>
        /// Registers a service with itself as a singleton when the service and implementation are the same type.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <returns>The container for method chaining.</returns>
        public IContainer RegisterService<TService>()
            => container.RegisterService<TService, TService>();
    }
}
