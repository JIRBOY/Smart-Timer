using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SmartTimer.Models;
using SmartTimer.Services;

namespace SmartTimer.UI;

/// <summary>
/// 托盘应用上下文 - 整合falcon和granite的优点
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;
    private readonly TimerEngine _engine;
    private ManagerForm? _manager;

    public TrayApplicationContext()
    {
        _engine = TimerEngine.Instance;
        _engine.TimerFired += OnTimerFired;

        _menu = BuildMenu();
        _tray = new NotifyIcon
        {
            Icon = BuildTrayIcon(),
            Text = "SmartTimer",
            Visible = true,
            ContextMenuStrip = _menu
        };

        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ToggleMute();
        };
        _tray.DoubleClick += (_, _) => OpenManager();

        // 启动引擎
        _engine.Start();

        // 首次运行提示
        if (_engine.Timers.Count == 0)
        {
            _tray.BalloonTipTitle = "SmartTimer 已启动";
            _tray.BalloonTipText = "右键托盘图标即可管理定时，双击打开管理窗口。";
            _tray.BalloonTipIcon = ToolTipIcon.Info;
            _tray.ShowBalloonTip(3000);
        }
    }

    #region 菜单构建

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false };
        menu.Opening += (_, _) => RebuildDynamicMenu(menu);
        return menu;
    }

    private void RebuildDynamicMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();

        // 标题
        var header = new ToolStripLabel("SmartTimer")
        {
            Font = new Font(SystemFonts.MenuFont!.FontFamily, 9.5f, FontStyle.Bold),
            ForeColor = Color.DimGray
        };
        menu.Items.Add(header);
        menu.Items.Add(new ToolStripSeparator());

        // 打开管理窗口
        menu.Items.Add("定时管理(&M)...", null, (_, _) => OpenManager());

        // 即将触发列表
        var upcoming = _engine.GetUpcomingTimers(5);
        var nextMenu = new ToolStripMenuItem("即将触发");
        if (upcoming.Count == 0)
        {
            nextMenu.DropDownItems.Add(new ToolStripMenuItem("（没有活动的定时）") { Enabled = false });
        }
        else
        {
            foreach (var t in upcoming)
            {
                var remain = t.NextTrigger - DateTime.Now;
                var label = $"{t.Name}  ·  {t.GetNextTriggerText()}  ·  {t.NextTrigger:MM-dd HH:mm:ss}";
                var captured = t;
                nextMenu.DropDownItems.Add(label, null, (_, _) => OpenManager(captured.Id));
            }
        }
        menu.Items.Add(nextMenu);

        // 快速添加
        var quick = new ToolStripMenuItem("快速添加(&Q)");
        foreach (var preset in new (string Label, int Minutes)[]
        {
            ("5 分钟后", 5), ("10 分钟后", 10), ("15 分钟后", 15),
            ("30 分钟后", 30), ("60 分钟后", 60)
        })
        {
            int minutes = preset.Minutes;
            string label = preset.Label;
            quick.DropDownItems.Add(label, null, (_, _) => QuickAdd(minutes));
        }
        menu.Items.Add(quick);

        menu.Items.Add(new ToolStripSeparator());

        // 静音开关
        var mute = new ToolStripMenuItem("全部静音", null, (_, _) => ToggleMute())
        {
            Checked = _engine.Settings.GlobalMute,
            CheckOnClick = false
        };
        menu.Items.Add(mute);

        // 开机自启
        var autostart = new ToolStripMenuItem("开机启动", null, (_, _) =>
        {
            var enable = !AutoStartService.IsEnabled();
            AutoStartService.SetEnabled(enable);
            _engine.Settings.AutoStart = enable;
            _engine.Save();
        })
        {
            Checked = AutoStartService.IsEnabled(),
            CheckOnClick = true
        };
        menu.Items.Add(autostart);

        menu.Items.Add(new ToolStripSeparator());
        //menu.Items.Add("关于(&A)...", null, (_, _) => ShowAbout());
        menu.Items.Add("退出(&X)", null, (_, _) => ExitApp());
    }

    #endregion

    #region 事件处理

    private void OnTimerFired(object? sender, TimerItem item)
    {
        // 播放声音
        SoundService.Play(
            item.CustomSoundPath ?? _engine.Settings.DefaultSoundPath,
            _engine.Settings.GlobalMute,
            _engine.Settings.Volume);

        // 显示提醒弹窗
        if (item.ShowPopup)
        {
            var reminder = new ReminderForm(item, _engine);
            reminder.Show();
            reminder.BringToFront();
        }
        else
        {
            // 仅显示托盘气泡
            _tray.BalloonTipTitle = item.Name;
            _tray.BalloonTipText = string.IsNullOrWhiteSpace(item.Message) ? "时间到！" : item.Message;
            _tray.BalloonTipIcon = ToolTipIcon.Info;
            _tray.ShowBalloonTip(4000);
        }
    }

    private void ToggleMute()
    {
        _engine.Settings.GlobalMute = !_engine.Settings.GlobalMute;
        _engine.Save();
        _tray.Icon = BuildTrayIcon();
        _tray.Text = _engine.Settings.GlobalMute
            ? "SmartTimer - 已静音"
            : "SmartTimer - 智能定时提醒器";
    }

    private void QuickAdd(int minutes)
    {
        var timer = new TimerItem
        {
            Name = $"{minutes}分钟提醒",
            Type = TimerType.OneTime,
            TargetTime = DateTime.Now.AddMinutes(minutes),
            Message = $"{minutes}分钟时间到！",
            SoundEnabled = _engine.Settings.DefaultSoundEnabled,
            ShowPopup = true,
            Enabled = true
        };
        _engine.Add(timer);

        _tray.BalloonTipTitle = "已添加定时";
        _tray.BalloonTipText = $"{timer.Name} 将在 {timer.TargetTime:HH:mm:ss} 触发";
        _tray.BalloonTipIcon = ToolTipIcon.Info;
        _tray.ShowBalloonTip(2500);
    }

    #endregion

    #region 窗口管理

    public void OpenManager(string? selectId = null)
    {
        if (_manager == null || _manager.IsDisposed)
            _manager = new ManagerForm(_engine);

        if (!_manager.Visible) _manager.Show();
        _manager.WindowState = FormWindowState.Normal;
        _manager.BringToFront();
        _manager.Activate();

        if (selectId != null)
            _manager.SelectTimer(selectId);
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "SmartTimer v1.0\n\n" +
            "整合多模型优点的智能定时提醒工具\n\n" +
            "功能特性：\n" +
            "• 支持一次性、周期、每日、每周定时\n" +
            "• 声音提醒和自定义铃声\n" +
            "• 稍后提醒（Snooze）\n" +
            "• 全局静音\n" +
            "• 开机自启动\n" +
            "• 数据持久化到 AppData",
            "关于 SmartTimer",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    #endregion

    #region 托盘图标

    private Icon BuildTrayIcon()
    {
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // 表盘颜色根据静音状态变化
            var faceBrush = _engine?.Settings.GlobalMute == true
                ? Brushes.Gray : Brushes.DodgerBlue;
            g.FillEllipse(faceBrush, 2, 2, 28, 28);
            g.DrawEllipse(new Pen(Color.White, 2), 2, 2, 28, 28);

            // 指针
            using var pen = new Pen(Color.White, 2);
            g.DrawLine(pen, 16, 16, 16, 7);   // 分针
            g.DrawLine(pen, 16, 16, 23, 19);  // 时针
            g.FillEllipse(Brushes.White, 14, 14, 4, 4);

            // 静音状态显示红色斜线
            if (_engine?.Settings.GlobalMute == true)
            {
                using var redPen = new Pen(Color.OrangeRed, 3);
                g.DrawLine(redPen, 4, 28, 28, 4);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        bmp.Dispose();
        return icon;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    #endregion

    #region 退出

    private void ExitApp()
    {
        var result = MessageBox.Show(
            "确定要退出 SmartTimer 吗？\n退出后所有定时将停止运行。",
            "确认退出",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _engine.Dispose();
            ExitThread();
        }
    }

    #endregion
}
