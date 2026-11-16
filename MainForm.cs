using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace Reminder30
{
    public sealed class MainForm : Form
    {
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(30);

        private readonly Label _titleLabel;
        private readonly Label _countdownLabel;
        private readonly Label _statusLabel;
        private readonly Button _startButton;
        private readonly Button _stopButton;
        private readonly NumericUpDown _minutesInput;
        private readonly Label _minutesLabel;
        private readonly Timer _timer;
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly ToolStripMenuItem _showMenuItem;
        private readonly ToolStripMenuItem _startMenuItem;
        private readonly ToolStripMenuItem _stopMenuItem;
        private readonly ToolStripMenuItem _exitMenuItem;
        private readonly Timer _alarmTimer;

        private DateTime _endTime;
        private bool _running;
        private bool _allowClose;
        private bool _alertShowing;
        private bool _startupMinimized;

        public MainForm()
        {
            Text = "定时提醒";
            ClientSize = new Size(420, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            _titleLabel = new Label
            {
                AutoSize = false,
                Text = "定时提醒",
                Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(20, 18, 380, 40)
            };

            _countdownLabel = new Label
            {
                AutoSize = false,
                Text = FormatRemaining(DefaultDuration),
                Font = new Font("Consolas", 28F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(20, 65, 380, 60)
            };

            _statusLabel = new Label
            {
                AutoSize = false,
                Text = "状态：未启动",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(20, 130, 380, 25)
            };

            _minutesLabel = new Label
            {
                AutoSize = true,
                Text = "提醒间隔（分钟）：",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular),
                Location = new Point(65, 170)
            };

            _minutesInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 240,
                Value = 30,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular),
                TextAlign = HorizontalAlignment.Center,
                Location = new Point(200, 166),
                Size = new Size(70, 25)
            };

            _startButton = new Button
            {
                Text = "开始",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Location = new Point(95, 205),
                Size = new Size(100, 32)
            };
            _startButton.Click += StartButton_Click;

            _stopButton = new Button
            {
                Text = "停止",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Location = new Point(225, 205),
                Size = new Size(100, 32),
                Enabled = false
            };
            _stopButton.Click += StopButton_Click;

            Controls.AddRange(new Control[]
            {
                _titleLabel,
                _countdownLabel,
                _statusLabel,
                _minutesLabel,
                _minutesInput,
                _startButton,
                _stopButton
            });

            _timer = new Timer { Interval = 1000 };
            _timer.Tick += Timer_Tick;
            _alarmTimer = new Timer { Interval = 1000 };
            _alarmTimer.Tick += AlarmTimer_Tick;

            _showMenuItem = new ToolStripMenuItem("显示窗口");
            _showMenuItem.Click += ShowMenuItem_Click;

            _startMenuItem = new ToolStripMenuItem("开始提醒");
            _startMenuItem.Click += StartMenuItem_Click;

            _stopMenuItem = new ToolStripMenuItem("停止提醒")
            {
                Enabled = false
            };
            _stopMenuItem.Click += StopMenuItem_Click;

            _exitMenuItem = new ToolStripMenuItem("退出");
            _exitMenuItem.Click += ExitMenuItem_Click;

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.AddRange(new ToolStripItem[]
            {
                _showMenuItem,
                _startMenuItem,
                _stopMenuItem,
                new ToolStripSeparator(),
                _exitMenuItem
            });

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Text = "30分钟定时提醒",
                Visible = true,
                ContextMenuStrip = _trayMenu
            };
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            Resize += MainForm_Resize;
            FormClosing += MainForm_FormClosing;
            Shown += MainForm_Shown;
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            var selectedDuration = TimeSpan.FromMinutes((double)_minutesInput.Value);
            _endTime = DateTime.Now.Add(selectedDuration);
            _running = true;
            _timer.Start();

            _minutesInput.Enabled = false;
            _startButton.Enabled = false;
            _stopButton.Enabled = true;
            UpdateCountdown();
            _statusLabel.Text = $"状态：运行中，将在 {selectedDuration.TotalMinutes:0} 分钟后提醒";
            SyncTrayState();
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            StopReminder("状态：已停止");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (!_running)
            {
                return;
            }

            var remaining = _endTime - DateTime.Now;
            if (remaining > TimeSpan.Zero)
            {
                _countdownLabel.Text = FormatRemaining(remaining);
                return;
            }

            _countdownLabel.Text = "00:00";
            if (_alertShowing)
            {
                return;
            }

            _alertShowing = true;
            _timer.Stop();
            StartAlarmLoop();
            ShowReminderWindow();
            _statusLabel.Text = "状态：时间已到，正在响铃提醒";

            var intervalMinutes = (double)_minutesInput.Value;
            _endTime = DateTime.Now.AddMinutes(intervalMinutes);
            _notifyIcon.BalloonTipTitle = "定时提醒";
            _notifyIcon.BalloonTipText = "30分钟已到，请注意休息或处理下一项任务。";
            _notifyIcon.ShowBalloonTip(3000);
        }

        private void StopReminder(string status)
        {
            _timer.Stop();
            StopAlarmLoop();
            _alertShowing = false;
            _running = false;
            _minutesInput.Enabled = true;
            _startButton.Enabled = true;
            _stopButton.Enabled = false;
            _countdownLabel.Text = FormatRemaining(TimeSpan.FromMinutes((double)_minutesInput.Value));
            _statusLabel.Text = status;
            SyncTrayState();
        }

        private static string FormatRemaining(TimeSpan remaining)
        {
            var totalSeconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                return;
            }

            Hide();
            _notifyIcon.BalloonTipTitle = "30分钟定时提醒";
            _notifyIcon.BalloonTipText = "程序已最小化到系统托盘。";
            _notifyIcon.ShowBalloonTip(2000);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowClose)
            {
                StopAlarmLoop();
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _trayMenu.Dispose();
                return;
            }

            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
            Hide();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (!_running)
            {
                StartButton_Click(sender, EventArgs.Empty);
            }

            if (!_startupMinimized)
            {
                _startupMinimized = true;
                WindowState = FormWindowState.Minimized;
            }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowMenuItem_Click(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void StartMenuItem_Click(object sender, EventArgs e)
        {
            if (!_running)
            {
                StartButton_Click(sender, e);
            }
        }

        private void StopMenuItem_Click(object sender, EventArgs e)
        {
            if (_running)
            {
                StopReminder("状态：已停止");
            }
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            _allowClose = true;
            Close();
        }

        private void ShowMainWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Activate();
        }

        private void SyncTrayState()
        {
            _startMenuItem.Enabled = !_running;
            _stopMenuItem.Enabled = _running;
        }

        private void ShowReminderWindow()
        {
            Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            TopMost = true;
            Activate();
            BringToFront();
            Focus();
        }

        private void StartAlarmLoop()
        {
            SystemSounds.Exclamation.Play();
            _alarmTimer.Start();
        }

        private void StopAlarmLoop()
        {
            _alarmTimer.Stop();
        }

        private void AlarmTimer_Tick(object sender, EventArgs e)
        {
            SystemSounds.Exclamation.Play();
            if (_alertShowing)
            {
                ShowReminderWindow();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (_alertShowing)
            {
                StopAlarmLoop();
                _alertShowing = false;
                TopMost = false;
                if (_running)
                {
                    var intervalMinutes = (double)_minutesInput.Value;
                    _statusLabel.Text = $"状态：已提醒一次，下一次将在 {intervalMinutes:0} 分钟后提醒";
                    _timer.Start();
                    UpdateCountdown();
                }
            }
        }
    }
}
