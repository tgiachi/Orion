namespace OrionIrcd.Tests.Architecture;

using System.Xml.Linq;

public sealed class ProjectDependencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ServerCore_ShouldNotReferenceIrcProject()
    {
        List<string> references = ReadProjectReferences("src/OrionIrcd.Server.Core/OrionIrcd.Server.Core.csproj");

        Assert.DoesNotContain(@"..\OrionIrcd.IRC\OrionIrcd.IRC.csproj", references);
    }

    [Fact]
    public void IrcProject_ShouldNotReferenceServerProjects()
    {
        List<string> references = ReadProjectReferences("src/OrionIrcd.IRC/OrionIrcd.IRC.csproj");

        Assert.DoesNotContain(@"..\OrionIrcd.Server\OrionIrcd.Server.csproj", references);
        Assert.DoesNotContain(@"..\OrionIrcd.Server.Core\OrionIrcd.Server.Core.csproj", references);
    }

    [Fact]
    public void ServerExecutable_ShouldComposeServerCoreAndIrc()
    {
        List<string> references = ReadProjectReferences("src/OrionIrcd.Server/OrionIrcd.Server.csproj");

        Assert.Contains(@"..\OrionIrcd.Server.Core\OrionIrcd.Server.Core.csproj", references);
        Assert.Contains(@"..\OrionIrcd.IRC\OrionIrcd.IRC.csproj", references);
    }

    private static List<string> ReadProjectReferences(string projectPath)
    {
        string fullPath = Path.Combine(RepositoryRoot, projectPath);
        XDocument document = XDocument.Load(fullPath);

        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OrionIrcd.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate OrionIrcd repository root.");
    }
}
