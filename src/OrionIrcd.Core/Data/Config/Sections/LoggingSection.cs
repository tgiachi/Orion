using OrionIrcd.Core.Types;

namespace OrionIrcd.Core.Data.Config.Sections;

public class LoggingSection
{
    public bool LogToConsole { get; set; } = true;

    public bool LogToFile { get; set; } = false;

    public LogLevelType LogLevel { get; set; } = LogLevelType.Information;
}
