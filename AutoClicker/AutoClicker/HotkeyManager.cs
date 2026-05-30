using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClicker
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_NONE = 0x0000;
        private const int HOTKEY_ID = 9000;
        private IntPtr _handle;
        private bool _registered = false;

        public event EventHandler? HotkeyPressed;

        public HotkeyManager(IntPtr handle)
        {
            _handle = handle;
        }

        public bool RegisterF6()
        {
            _registered = RegisterHotKey(_handle, HOTKEY_ID, MOD_NONE, 0x75);
            return _registered;
        }

        public void Unregister()
        {
            if (_registered)
            {
                UnregisterHotKey(_handle, HOTKEY_ID);
                _registered = false;
            }
        }

        public void ProcessMessage(Message m)
        {
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}
