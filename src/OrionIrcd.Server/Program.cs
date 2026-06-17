using ConsoleAppFramework;
using DryIoc;
using OrionIrcd.Core.Data.Config;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Extensions.Directories;
using OrionIrcd.Core.Extensions.Logger;
using OrionIrcd.Core.Types;
using OrionIrcd.Core.Yaml;
using Serilog;

var container = new Container();

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

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
);

static OrionIrcdConfig LoadConfig(Container container, string configFileName = "orionircd.yaml")
{
    var directoriesConfig = container.Resolve<DirectoriesConfig>();
    var configFullFileName = Path.Combine(directoriesConfig.Root, configFileName);

    if (File.Exists(configFullFileName))
    {
        return YamlUtils.DeserializeFromFile<OrionIrcdConfig>(configFullFileName);
    }

    Console.WriteLine("Initializing default config " + configFileName);

    var config = new OrionIrcdConfig();

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

    container.RegisterInstance(config);
    container.RegisterInstance(config.Network);
    container.RegisterInstance(config.Logging);

    return config;
}
