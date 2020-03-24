using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CoreMeter
{
    public class CoreMeterUtility
    {
        private int _bottomZOrder = 0;
        private IntPtr _hWnd;

        private CancellationTokenSource _cancellationTokenSource;

        public CoreMeterUtility(IntPtr hWnd, bool hideFromAltTabMenu = true)
        {
            _hWnd = hWnd;
            _cancellationTokenSource = new CancellationTokenSource();

            if (hideFromAltTabMenu)
                HideFromAltTabMenu(_hWnd);
        }

        public async void Lock(int? x = null, int? y = null, int? width = null, int? height = null)
        {
            await Lock(_cancellationTokenSource.Token, x, y, width, height);
        }

        public void Unlock()
        {
            _cancellationTokenSource?.Cancel();

            _cancellationTokenSource = new CancellationTokenSource();
        }

        private async Task Lock(CancellationToken cancellationToken, int? x, int? y, int? width, int? height)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                bool update = false;

                int zOrder = GetZOrder(_hWnd);

                RECT windowRect;

                GetWindowRect(_hWnd, out windowRect);

                // Verbosity is a good way to make friends

                if (zOrder != _bottomZOrder)
                    update = true;

                if (x != null)
                    if (!windowRect.Left.Equals(x))
                        update = true;

                if (y != null)
                    if (!windowRect.Top.Equals(y))
                        update = true;

                if (width != null)
                    if (!(windowRect.Right - windowRect.Left).Equals(width))
                        update = true;

                if (height != null)
                    if (!(windowRect.Right - windowRect.Left).Equals(height))
                        update = true;

                if (update)
                {
                    SendWindowToBottom(_hWnd, x ?? windowRect.Left, y ?? windowRect.Top, width ?? (windowRect.Right - windowRect.Left), height ?? (windowRect.Right - windowRect.Left));
                    _bottomZOrder = GetZOrder(_hWnd);
                }

                await Task.Delay(100);
            }
        }

        #region Win32 Interop
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static int GetZOrder(IntPtr hWnd)
        {
            const uint GW_HWNDNEXT = 2;

            var z = 0;

            for (var h = hWnd; h != IntPtr.Zero; h = GetWindow(h, GW_HWNDNEXT))
                z++;

            return z;
        }

        private static void SendWindowToBottom(IntPtr hWnd, int x, int y, int width, int height)
        {
            const int SWP_SHOWWINDOW = 0x0040;
            const int SWP_NOACTIVATE = 0x0010;

            const int HWND_BOTTOM = 0x1;

            Debug.WriteLine(x);

            SetWindowPos(hWnd, HWND_BOTTOM, x, y, width, height, SWP_SHOWWINDOW | SWP_NOACTIVATE);
        }

        private static void HideFromAltTabMenu(IntPtr hWnd)
        {
            const int GWL_EX_STYLE = -20;
            const int WS_EX_TOOLWINDOW = 0x00000080;

            const int GWL_STYLE = -16;

            SetWindowLong(hWnd, GWL_EX_STYLE, WS_EX_TOOLWINDOW);
            SetWindowLong(hWnd, GWL_STYLE, 0);
        }
        #endregion
    }
}
