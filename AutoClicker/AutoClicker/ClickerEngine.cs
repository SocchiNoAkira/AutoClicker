using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoClicker
{
    public class ClickerEngine : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const uint INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray)] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        private BackgroundWorker? _worker;
        private volatile bool _isRunning = false;

        public int IntervalMs { get; set; } = 100;
        public int DurationSeconds { get; set; } = 10;
        public bool IsForever { get; set; } = false;
        public BindingList<ClickPosition> Positions { get; } = new BindingList<ClickPosition>();
        public bool UseCurrentPosition { get; set; } = true;

        public event EventHandler? Started;
        public event EventHandler? Stopped;
        public event EventHandler<ClickEventArgs>? ClickPerformed;

        public bool IsRunning => _isRunning;

        public void Start()
        {
            if (_isRunning) return;

            _worker = new BackgroundWorker();
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += Worker_DoWork;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            _worker.RunWorkerAsync();
        }

        public void Stop()
        {
            if (_worker != null && _worker.IsBusy)
            {
                _worker.CancelAsync();
            }
        }

        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            _isRunning = true;
            Started?.Invoke(this, EventArgs.Empty);

            DateTime startTime = DateTime.Now;
            int posIndex = 0;

            while (!_worker!.CancellationPending)
            {
                if (!IsForever && (DateTime.Now - startTime).TotalSeconds >= DurationSeconds)
                    break;

                PerformClick(posIndex);
                posIndex = Positions.Count > 0 ? (posIndex + 1) % Positions.Count : 0;

                ClickPerformed?.Invoke(this, new ClickEventArgs { PositionIndex = posIndex });

                Thread.Sleep(IntervalMs);
            }

            e.Cancel = true;
        }

        private void PerformClick(int posIndex)
        {
            if (Positions.Count > 0 && !UseCurrentPosition)
            {
                var pos = Positions[posIndex];
                SetCursorPos(pos.X, pos.Y);
            }

            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

            inputs[1].type = INPUT_MOUSE;
            inputs[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            _isRunning = false;
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public class ClickPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public override string ToString() => $"({X}, {Y})";
    }

    public class ClickEventArgs : EventArgs
    {
        public int PositionIndex { get; set; }
    }
}
