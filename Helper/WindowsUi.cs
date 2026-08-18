// TotalMix ウィンドウの表示切替 (Windows)

namespace streamdeck_totalmix
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;

    [SupportedOSPlatform("windows")]
    internal static class WindowsUi
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        private const int SW_HIDE = 0;
        private const int SW_RESTORE = 5;

        public static void ShowHideUi()
        {
            Process[] p = Process.GetProcessesByName(Platform.ProcessName(TargetPlatform.Windows));
            IntPtr hWnd = p[0].MainWindowHandle;
            if (hWnd == IntPtr.Zero)
            {
                // 非表示中は MainWindowHandle が 0 になるので、スレッドが持つウィンドウから復帰先を拾う。
                // ハンドルは TotalMix の再起動で変わるため、呼ばれるたびに取り直す (キャッシュしない)。
                ShowWindowAsync(EnumerateProcessWindowHandles(p[0].Id).First(), SW_RESTORE);
            }
            else
            {
                ShowWindowAsync(hWnd, SW_HIDE);
            }
        }

        private static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                EnumThreadWindows(thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }
    }
}
