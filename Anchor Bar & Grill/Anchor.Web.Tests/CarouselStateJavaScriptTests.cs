using System.Diagnostics;

namespace Anchor.Web.Tests;

public sealed class CarouselStateJavaScriptTests
{
    [Fact]
    public async Task CarouselStateTransitions_HandleNavigationAndBoundaries()
    {
        var repositoryRoot = GetRepositoryRoot();
        var testFile = Path.Combine(repositoryRoot, "Anchor Bar & Grill", "Anchor.Web.Tests", "JavaScript", "carousel-state.test.cjs");
        var startInfo = new ProcessStartInfo("node", $"\"{testFile}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.True(process.ExitCode == 0, $"Carousel JavaScript tests failed.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null
            && !Directory.Exists(Path.Combine(directory.FullName, ".git"))
            && !File.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
