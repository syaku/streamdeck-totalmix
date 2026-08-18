using System.IO;
using streamdeck_totalmix;

namespace streamdeck_totalmix.Tests;

public class PlatformTests
{
    [Fact]
    public void WindowsProcessNameIsTotalMixFX()
    {
        Assert.Equal("TotalMixFX", Platform.ProcessName(TargetPlatform.Windows));
    }

    [Fact]
    public void MacProcessNameIsTotalmixFXWithLowercaseM()
    {
        Assert.Equal("TotalmixFX", Platform.ProcessName(TargetPlatform.MacOs));
    }

    [Fact]
    public void WindowsStartUsesShellExecute()
    {
        var startInfo = Platform.StartInfo(TargetPlatform.Windows);

        Assert.True(startInfo.UseShellExecute);
        Assert.Equal("TotalMixFX.exe", startInfo.FileName);
    }

    [Fact]
    public void MacStartOpensTotalmixViaOpen()
    {
        var startInfo = Platform.StartInfo(TargetPlatform.MacOs);

        Assert.Equal("open", startInfo.FileName);
        Assert.Equal("-a Totalmix", startInfo.Arguments);
    }

    [Fact]
    public void WindowsSettingsDirectoryLivesUnderLocalAppData()
    {
        var directory = Platform.SettingsDirectory(
            TargetPlatform.Windows,
            localApplicationData: @"C:\Users\tester\AppData\Local",
            homeDirectory: "/Users/tester");

        Assert.StartsWith(@"C:\Users\tester\AppData\Local", directory);
        Assert.Equal("TotalMixFx", Path.GetFileName(directory));
    }

    [Fact]
    public void MacSettingsDirectoryLivesUnderApplicationSupport()
    {
        var directory = Platform.SettingsDirectory(
            TargetPlatform.MacOs,
            localApplicationData: @"C:\Users\tester\AppData\Local",
            homeDirectory: "/Users/tester");

        // 実行中の OS の区切り文字で組まれるため、文字列全体でなく階層で照合する
        // (windows-latest の CI でも同じ assert が通る)。
        Assert.StartsWith("/Users/tester", directory);
        Assert.Equal("RME TotalMix FX", Path.GetFileName(directory));
        Assert.Equal("Application Support", Path.GetFileName(Path.GetDirectoryName(directory)));
        Assert.Equal("Library", Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(directory))));
    }

    [Fact]
    public void MacRestartWaitsForTheOpenLauncherToExit()
    {
        Assert.True(Platform.LauncherExitsImmediately(TargetPlatform.MacOs));
    }

    [Fact]
    public void WindowsRestartDoesNotWaitForTheLaunchedApplication()
    {
        Assert.False(Platform.LauncherExitsImmediately(TargetPlatform.Windows));
    }

    [Fact]
    public void SnapshotNamesComeFromTheMostRecentSettingsFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            var stale = Path.Combine(directory, "last.0.xml");
            var current = Path.Combine(directory, "last.1.xml");
            File.WriteAllLines(stale, ["<SnapshotName v=\"Stale\"/>", "<Inputs>"]);
            File.WriteAllLines(current, ["<SnapshotName v=\"Studio\"/>", "<SnapshotName v=\"Live\"/>", "<Inputs>", "<SnapshotName v=\"AfterInputs\"/>"]);
            File.SetLastWriteTimeUtc(stale, File.GetLastWriteTimeUtc(current).AddMinutes(-5));

            var names = Platform.ReadSnapshotNames(directory);

            Assert.Equal(new[] { "Studio", "Live" }, names);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
