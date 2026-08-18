// TotalMix ウィンドウの表示切替 (macOS)

namespace streamdeck_totalmix
{
    using BarRaider.SdTools;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    internal static class MacUi
    {
        // user32 の ShowWindowAsync に相当するものが macOS には無いため、System Events の
        // visible を反転させる。初回実行時に Stream Deck アプリへ Automation 権限の許可が要る。
        private const String ToggleVisibleScript =
            "tell application \"System Events\" to tell process \"{0}\" to set visible to not visible";

        // 初回は Automation 権限の許可ダイアログが出て、人が応答するまで AppleEvent が返らない。
        // 呼び出し元 (Stream Deck のイベント処理) で待つと他アクションの更新まで止まるので待たせない。
        // Windows 側の ShowWindowAsync も即戻るため、待たない方が両 OS で挙動が揃う。
        private const Int32 ScriptTimeoutMilliseconds = 60000;

        public static void ShowHideUi()
        {
            Task.Run(() => RunToggleScript());
        }

        private static void RunToggleScript()
        {
            var script = String.Format(ToggleVisibleScript, Platform.ProcessName(TargetPlatform.MacOs));
            var startInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                UseShellExecute = false,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add(script);

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    var error = process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(ScriptTimeoutMilliseconds))
                    {
                        process.Kill(entireProcessTree: true);
                        Logger.Instance.LogMessage(TracingLevel.WARN, "MacUi: osascript timed out and was killed");
                        return;
                    }
                    if (process.ExitCode != 0)
                    {
                        var message = error.Wait(TimeSpan.FromSeconds(1)) ? error.Result : String.Empty;
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"MacUi: osascript exited {process.ExitCode}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "MacUi: ShowHideUi: " + ex.Message);
            }
        }
    }
}
