// OS 差分の集約。呼び出し側は OS を意識しない。

namespace streamdeck_totalmix
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public enum TargetPlatform
    {
        Windows,
        MacOs
    }

    public static class Platform
    {
        public static TargetPlatform Current => OperatingSystem.IsWindows() ? TargetPlatform.Windows : TargetPlatform.MacOs;

        public static String ProcessName() => ProcessName(Current);

        public static String ProcessName(TargetPlatform platform)
        {
            return platform == TargetPlatform.Windows ? "TotalMixFX" : "TotalmixFX";
        }

        public static ProcessStartInfo StartInfo() => StartInfo(Current);

        public static ProcessStartInfo StartInfo(TargetPlatform platform)
        {
            if (platform == TargetPlatform.Windows)
            {
                // UseShellExecute の既定が .NET Framework の true から net 系で false に変わり、
                // false のままだとベア名が PATH 探索でしか解決されず TotalMix を起動できない。
                return new ProcessStartInfo { FileName = "TotalMixFX.exe", UseShellExecute = true };
            }
            return new ProcessStartInfo { FileName = "open", Arguments = "-a Totalmix", UseShellExecute = false };
        }

        public static Boolean LauncherExitsImmediately() => LauncherExitsImmediately(Current);

        // mac の open はアプリを起こすだけのランチャなのですぐ終了し、終了コードで失敗を判定できる。
        // Windows で起動するのは TotalMix 本体なので、待つとアプリを閉じるまで戻らない。
        public static Boolean LauncherExitsImmediately(TargetPlatform platform)
        {
            return platform == TargetPlatform.MacOs;
        }

        public static String SettingsDirectory()
        {
            return SettingsDirectory(
                Current,
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        public static String SettingsDirectory(TargetPlatform platform, String localApplicationData, String homeDirectory)
        {
            return platform == TargetPlatform.Windows
                ? Path.Combine(localApplicationData, "TotalMixFx")
                : Path.Combine(homeDirectory, "Library", "Application Support", "RME TotalMix FX");
        }

        public static List<String> ReadSnapshotNames(String settingsDirectory)
        {
            var names = new List<String>();
            if (!Directory.Exists(settingsDirectory))
            {
                return names;
            }

            var currentConfig = Directory.GetFiles(settingsDirectory, "last.*.xml")
                .Select(x => new FileInfo(x))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();
            if (currentConfig == null)
            {
                return names;
            }

            foreach (var line in File.ReadAllLines(currentConfig.FullName))
            {
                if (line.Contains("SnapshotName"))
                {
                    Match snapshotNames = Regex.Match(line, "v\\=\\\"(.*\\b)");
                    if (snapshotNames.Success)
                    {
                        names.Add(snapshotNames.Groups[1].Value);
                    }
                }
                if (line.Contains("<Inputs>"))
                {
                    return names;
                }
            }
            return names;
        }
    }
}
