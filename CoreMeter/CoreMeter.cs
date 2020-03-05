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

        public CoreMeterUtility(IntPtr hWnd)
        {
            _hWnd = hWnd;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async void Lock()
        {
            await Lock(_cancellationTokenSource.Token);
        }

        public void Unlock()
        {
            _cancellationTokenSource?.Cancel();

            _cancellationTokenSource = new CancellationTokenSource();
        }

        private async Task Lock(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int zOrder = GetZOrder(_hWnd);

                Debug.WriteLine(zOrder);

                if (zOrder != _bottomZOrder)
                {
                    SendWindowToBottom(_hWnd);
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

        int GetZOrder(IntPtr hWnd)
        {
            const uint GW_HWNDNEXT = 2;

            var z = 0;

            for (var h = hWnd; h != IntPtr.Zero; h = GetWindow(h, GW_HWNDNEXT))
                z++;

            return z;
        }

        private static void SendWindowToBottom(IntPtr hWnd)
        {
            const int SWP_NOMOVE = 0x2;
            const int SWP_NOSIZE = 0x1;
            const int SWP_SHOWWINDOW = 0x0040;
            const int SWP_NOACTIVATE = 0x0010;

            const int HWND_BOTTOM = 0x1;

            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOMOVE | SWP_NOACTIVATE);
        }
        #endregion
    }
}
