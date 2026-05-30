using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace AutoClicker
{
    public class MainForm : Form
    {
        private ClickerEngine _clicker = null!;
        private HotkeyManager _hotkey = null!;
        private TrayManager _tray = null!;

        // UI Controls
        private NumericUpDown numFrequency = null!;
        private NumericUpDown numDuration = null!;
        private CheckBox chkForever = null!;
        private ListBox lstPositions = null!;
        private Button btnAddPos = null!;
        private Button btnRemovePos = null!;
        private Button btnStart = null!;
        private Button btnStop = null!;
        private Label lblStatus = null!;
        private RadioButton radCurrentPos = null!;
        private RadioButton radFixedPos = null!;

        // 全屏选点用的覆盖层
        private Form? _overlay = null;
        private System.Windows.Forms.Timer? _overlayTimeout = null;

        public MainForm()
        {
            Text = "鼠标连点器";
            Size = new Size(500, 440);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MinimizeBox = true;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            InitializeComponents();
            InitializeEngine();
        }

        private void InitializeComponents()
        {
            Font = new Font("Microsoft YaHei", 10);

            // ── 频率设置 ──
            var lblFreq = new Label { Text = "点击频率 (次/秒):", Location = new Point(20, 20), AutoSize = true };
            numFrequency = new NumericUpDown
            {
                Location = new Point(180, 18),
                Size = new Size(80, 25),
                Minimum = 1,
                Maximum = 1000,
                Value = 10
            };

            // ── 持续时间 ──
            var lblDuration = new Label { Text = "持续时间 (秒):", Location = new Point(20, 55), AutoSize = true };
            numDuration = new NumericUpDown
            {
                Location = new Point(180, 53),
                Size = new Size(80, 25),
                Minimum = 1,
                Maximum = 999999,
                Value = 10
            };
            chkForever = new CheckBox { Text = "永久持续", Location = new Point(280, 55), AutoSize = true };

            // ── 位置选择 ──
            var lblPos = new Label { Text = "点击位置:", Location = new Point(20, 90), AutoSize = true };
            radCurrentPos = new RadioButton
            {
                Text = "当前鼠标位置",
                Location = new Point(120, 90),
                AutoSize = true,
                Checked = true
            };
            radFixedPos = new RadioButton
            {
                Text = "固定位置",
                Location = new Point(250, 90),
                AutoSize = true
            };

            // ── 位置列表 ──
            lstPositions = new ListBox
            {
                Location = new Point(20, 125),
                Size = new Size(300, 150),
                SelectionMode = SelectionMode.One
            };

            // ── 位置操作按钮 ──
            btnAddPos = new Button { Text = "+ 点击选取", Location = new Point(340, 125), Size = new Size(110, 35) };
            btnRemovePos = new Button { Text = "- 删除选中", Location = new Point(340, 170), Size = new Size(110, 35) };

            // ── 主控制按钮 ──
            btnStart = new Button
            {
                Text = "▶ 开始 (F6)",
                Location = new Point(20, 300),
                Size = new Size(200, 45),
                BackColor = Color.LightGreen,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold)
            };
            btnStop = new Button
            {
                Text = "⏹ 停止 (F6)",
                Location = new Point(240, 300),
                Size = new Size(200, 45),
                BackColor = Color.LightCoral,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                Enabled = false
            };

            // ── 状态栏 ──
            lblStatus = new Label
            {
                Text = "就绪 - 按 F6 开始/停止",
                Location = new Point(20, 360),
                Size = new Size(420, 25),
                BorderStyle = BorderStyle.FixedSingle
            };

            Controls.AddRange(new Control[] {
                lblFreq, numFrequency,
                lblDuration, numDuration, chkForever,
                lblPos, radCurrentPos, radFixedPos,
                lstPositions, btnAddPos, btnRemovePos,
                btnStart, btnStop, lblStatus
            });

            // 事件绑定
            btnAddPos.Click += BtnAddPos_Click;
            btnRemovePos.Click += BtnRemovePos_Click;
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            radCurrentPos.CheckedChanged += RadPos_CheckedChanged;
            radFixedPos.CheckedChanged += RadPos_CheckedChanged;
            chkForever.CheckedChanged += ChkForever_CheckedChanged;
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;

            ChkForever_CheckedChanged(this, EventArgs.Empty);
            RadPos_CheckedChanged(this, EventArgs.Empty);
        }

        private void InitializeEngine()
        {
            _clicker = new ClickerEngine();
            _clicker.Started += (s, e) => UpdateUIState(true);
            _clicker.Stopped += (s, e) => UpdateUIState(false);
            _clicker.ClickPerformed += (s, e) =>
            {
                Invoke(new Action(() =>
                {
                    lblStatus.Text = _clicker.Positions.Count > 0
                        ? $"连点中... 位置 #{e.PositionIndex + 1}"
                        : "连点中...";
                }));
            };

            _hotkey = new HotkeyManager(this.Handle);
            _hotkey.RegisterF6();
            _hotkey.HotkeyPressed += (s, e) => ToggleClicking();

            _tray = new TrayManager();
            _tray.ShowWindowClicked += (s, e) => { Show(); WindowState = FormWindowState.Normal; };
            _tray.StartClicked += (s, e) => BtnStart_Click(this, EventArgs.Empty);
            _tray.StopClicked += (s, e) => BtnStop_Click(this, EventArgs.Empty);
            _tray.ExitClicked += (s, e) => { _clicker.Dispose(); Application.Exit(); };
        }

        // ── 点击屏幕选取位置 ──
        private void BtnAddPos_Click(object? sender, EventArgs e)
        {
            // 创建全屏半透明覆盖层
            _overlay = new OverlayForm();

            // 覆盖所有屏幕
            var bounds = Screen.AllScreens[0].Bounds;
            foreach (Screen screen in Screen.AllScreens)
            {
                bounds = Rectangle.Union(bounds, screen.Bounds);
            }
            _overlay.Bounds = bounds;

            _overlay.MouseClick += Overlay_MouseClick;
            _overlay.FormClosing += (s, args) => CloseOverlay();
            _overlay.Show();

            // 1分钟超时自动关闭
            _overlayTimeout = new System.Windows.Forms.Timer { Interval = 60000 };
            _overlayTimeout.Tick += (s, args) =>
            {
                if (_overlay != null)
                {
                    _overlay.Close();
                }
            };
            _overlayTimeout.Start();

            // 主窗口隐藏避免干扰
            this.Hide();
        }

        private void CloseOverlay()
        {
            if (_overlayTimeout != null)
            {
                _overlayTimeout.Stop();
                _overlayTimeout.Dispose();
                _overlayTimeout = null;
            }
            _overlay = null;
            this.Show();
            this.Activate();
        }

        private void Overlay_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_overlay == null) return;

            if (e.Button == MouseButtons.Left)
            {
                // 屏幕坐标
                var pos = new ClickPosition { X = e.X, Y = e.Y };
                _clicker.Positions.Add(pos);
                lstPositions.Items.Add(pos);
                radFixedPos.Checked = true;

                _overlay.Close();
                // CloseOverlay 由 FormClosing 事件调用

                lblStatus.Text = $"已添加位置 ({pos.X}, {pos.Y}) - 继续点击可添加更多";
            }
            else if (e.Button == MouseButtons.Right)
            {
                _overlay.Close();
                // CloseOverlay 由 FormClosing 事件调用
            }
        }

        private void BtnRemovePos_Click(object? sender, EventArgs e)
        {
            if (lstPositions.SelectedIndex >= 0)
            {
                _clicker.Positions.RemoveAt(lstPositions.SelectedIndex);
                lstPositions.Items.RemoveAt(lstPositions.SelectedIndex);
            }
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (_clicker.IsRunning) return;

            _clicker.IntervalMs = 1000 / (int)numFrequency.Value;
            _clicker.DurationSeconds = (int)numDuration.Value;
            _clicker.IsForever = chkForever.Checked;
            _clicker.UseCurrentPosition = radCurrentPos.Checked;

            _clicker.Start();
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            _clicker.Stop();
        }

        private void ToggleClicking()
        {
            if (_clicker.IsRunning)
                BtnStop_Click(this, EventArgs.Empty);
            else
                BtnStart_Click(this, EventArgs.Empty);
        }

        private void UpdateUIState(bool isRunning)
        {
            Invoke(new Action(() =>
            {
                btnStart.Enabled = !isRunning;
                btnStop.Enabled = isRunning;
                lblStatus.Text = isRunning ? "连点中..." : "已停止";

                if (isRunning)
                    _tray.ShowBalloon("连点器", "开始连点", ToolTipIcon.Info);
                else
                    _tray.ShowBalloon("连点器", "已停止", ToolTipIcon.Info);
            }));
        }

        private void RadPos_CheckedChanged(object? sender, EventArgs e)
        {
            bool fixedMode = radFixedPos.Checked;
            lstPositions.Enabled = fixedMode;
            btnAddPos.Enabled = fixedMode;
            btnRemovePos.Enabled = fixedMode;
        }

        private void ChkForever_CheckedChanged(object? sender, EventArgs e)
        {
            numDuration.Enabled = !chkForever.Checked;
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                _tray.ShowBalloon("鼠标连点器", "程序已最小化到托盘", ToolTipIcon.Info);
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _overlay?.Close();
            _clicker.Dispose();
            _hotkey.Dispose();
            _tray.Dispose();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            _hotkey?.ProcessMessage(m);
        }
    }

    /// <summary>
    /// 全屏透明覆盖层，用于点击选取屏幕位置。
    /// 提示文字设为穿透鼠标事件，确保 Form 本身能收到点击。
    /// </summary>
    public class OverlayForm : Form
    {
        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            Opacity = 0.15;
            BackColor = Color.Black;
            Cursor = Cursors.Cross;

            // 提示标签 — 关键：Enabled=false 让鼠标事件穿透到 Form
            var tip = new Label
            {
                Text = "左键选取位置 | 右键取消 | 1分钟自动关闭",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 24, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Enabled = false  // 不拦截鼠标事件，穿透给 Form
            };
            Controls.Add(tip);
        }

        /// <summary>
        /// WS_EX_TRANSPARENT 让窗口本身也能穿透未被处理的事件。
        /// 但我们只希望 Label 穿透，Form 仍要接收点击，所以不设置此样式。
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // 不加 WS_EX_TRANSPARENT，否则 Form 也穿透了
                return cp;
            }
        }
    }
}
