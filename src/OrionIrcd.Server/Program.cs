using ConsoleAppFramework;
using DryIoc;
using OrionIrcd.Core.Container;
using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Extensions.Directories;
using OrionIrcd.Core.Extensions.Logger;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Core.Types;
using OrionIrcd.Core.Utils;
using OrionIrcd.Core.Yaml;
using OrionIrcd.Network.Interfaces.Processing;
using OrionIrcd.Server.Interfaces.Services;
using OrionIrcd.Server.Services;
using OrionIrcd.Server.Services.Events;
using OrionIrcd.Server.Services.Network;
using OrionIrcd.Server.Services.Sessions;
using Serilog;

var container = new Container();

container.RegisterService<IEventBus, EventBus>();
container.RegisterService<ISessionManagerService, SessionManagerService>(50);
container.Register<IResultProcessor<string>, StringProcessor>(Reuse.Singleton);
container.RegisterService<NetworkServerService, NetworkServerService>(100);

await ConsoleApp.RunAsync(
    args,
    async (CancellationToken cancellationToken, string? rootDirectory = null, bool printHeader = false) =>
    {
        rootDirectory ??= "~/orionircd".ResolvePathAndEnvs();

        Console.WriteLine("root directory: " + rootDirectory);

        var directoriesConfig = new DirectoriesConfig(rootDirectory, Enum.GetNames<DirectoryType>());

        container.RegisterInstance(directoriesConfig);

        var config = LoadConfig(container);

        var loggingConfiguration = new LoggerConfiguration().MinimumLevel.Is(config.Logging.LogLevel.ToSerilogLogLevel());

        if (config.Logging.LogToConsole)
        {
            loggingConfiguration = loggingConfiguration.WriteTo.Console();
        }

        if (config.Logging.LogToFile)
        {
            loggingConfiguration = loggingConfiguration.WriteTo.File(
                Path.Combine(directoriesConfig[DirectoryType.Logs], "orionircd_.log"),
                rollingInterval: RollingInterval.Day
            );
        }

        Log.Logger = loggingConfiguration.CreateLogger();

        container.Register<IOrionIrcdOrchestrator, OrionIrcdOrchestrator>();

        Log.Information("Starting up...");
        Log.Information(
            "OrionIRCd v{Version} Platform {Platform}",
            VersionUtils.GetVersion(),
            PlatformUtils.GetCurrentPlatform()
        );

        await container.Resolve<IOrionIrcdOrchestrator>().RunAsync(cancellationToken);
    }
);

static OrionIrcdConfig LoadConfig(Container container, string configFileName = "orionircd.yaml")
{
    var directoriesConfig = container.Resolve<DirectoriesConfig>();
    var configFullFileName = Path.Combine(directoriesConfig.Root, configFileName);

    OrionIrcdConfig config;

    if (File.Exists(configFullFileName))
    {
        config = YamlUtils.DeserializeFromFile<OrionIrcdConfig>(configFullFileName);
    }
    else
    {
        Console.WriteLine("Initializing default config " + configFileName);

        config = new();

        config.Network.Entries.Add(
            new()
            {
                IpAddress = "*",
                Mode = ServerModeType.Server,
                Ports = "6666-6668",
                Protocol = ServerProtocolType.Plain,
                Type = ServerType.TCP
            }
        );

        YamlUtils.SerializeToFile(config, configFullFileName);
    }

    container.RegisterInstance(config);
    container.RegisterInstance(config.Network);
    container.RegisterInstance(config.Logging);

    return config;
}
