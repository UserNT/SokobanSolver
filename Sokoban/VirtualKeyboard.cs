using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Sokoban
{
    public static class VirtualKeyboard
    {
        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);

        public static async Task Send(IntPtr hwnd, Tuple<int, Key, int> key)
        {
            SetForegroundWindow(hwnd);
            await Task.Delay(200);

            string code = Convert(key.Item2);

            if (!string.IsNullOrWhiteSpace(code))
            {
                SendKeys.SendWait(code);
            }
        }

        private static string Convert(Key key)
        {
            if (key == Key.F5) return "{F5}";
            if (key == Key.Left) return "{LEFT}";
            if (key == Key.Right) return "{RIGHT}";
            if (key == Key.Down) return "{DOWN}";
            if (key == Key.Up) return "{UP}";

            return null;
        }
    }
}
