using System;
using System.Drawing;
using System.Windows.Forms;
using SmartTimer.Models;
using SmartTimer.Services;

namespace SmartTimer.UI;

/// <summary>
/// 提醒弹窗 - 支持稍后提醒
/// </summary>
public class ReminderForm : Form
{
    private readonly TimerItem _timer;
    private readonly TimerEngine _engine;
    private readonly System.Windows.Forms.Timer _autoCloseTimer;

    public ReminderForm(TimerItem timer, TimerEngine engine)
    {
        _timer = timer;
        _engine = engine;

        Text = "定时提醒";
        Size = new Size(360, 275);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        TopMost = true;
        ShowInTaskbar = false;

        // 标题
        var lblTitle = new Label
        {
            Text = _timer.Name,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Left = 15,
            Top = 15,
            Width = 260,
            Height = 30
        };
        Controls.Add(lblTitle);

        // 消息
        var lblMessage = new Label
        {
            Text = _timer.Message,
            Font = new Font(Font.FontFamily, 11),
            Left = 15,
            Top = 55,
            Width = 260,
            Height = 70
        };
        Controls.Add(lblMessage);

        // 时间
        var lblTime = new Label
        {
            Text = $"触发时间: {DateTime.Now:HH:mm:ss}",
            ForeColor = Color.Gray,
            Left = 15,
            Top = 130,
            Width = 200
        };
        Controls.Add(lblTime);

        // 稍后提醒按钮
        var btnSnooze = new Button
        {
            Text = "稍后提醒(5分钟)",
            Left = 15,
            Top = 175,
            Width = 120,
            Height = 29
        };
        btnSnooze.Click += (_, _) =>
        {
            _engine.Snooze(_timer.Id, 5);
            Close();
        };
        Controls.Add(btnSnooze);

        // 关闭按钮
        var btnClose = new Button
        {
            Text = "关闭",
            Left = 150,
            Top = 175,
            Width = 80,
            Height = 29,
            DialogResult = DialogResult.OK
        };
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);

        // 自动关闭定时器
        _autoCloseTimer = new System.Windows.Forms.Timer { Interval = 30000 }; // 30秒
        _autoCloseTimer.Tick += (_, _) => Close();
        _autoCloseTimer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _autoCloseTimer.Stop();
        _autoCloseTimer.Dispose();
        base.OnFormClosing(e);
    }
}
