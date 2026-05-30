using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoClicker
{
    public class TrayManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;

        public event EventHandler? ShowWindowClicked;
        public event EventHandler? StartClicked;
        public event EventHandler? StopClicked;
        public event EventHandler? ExitClicked;

        public TrayManager()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("显示窗口", null, (s, e) => ShowWindowClicked?.Invoke(this, EventArgs.Empty));
            _contextMenu.Items.Add("开始连点", null, (s, e) => StartClicked?.Invoke(this, EventArgs.Empty));
            _contextMenu.Items.Add("停止连点", null, (s, e) => StopClicked?.Invoke(this, EventArgs.Empty));
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("退出", null, (s, e) => ExitClicked?.Invoke(this, EventArgs.Empty));

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "鼠标连点器",
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => ShowWindowClicked?.Invoke(this, EventArgs.Empty);
        }

        public void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon.ShowBalloonTip(2000, title, text, icon);
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
        }
    }
}
