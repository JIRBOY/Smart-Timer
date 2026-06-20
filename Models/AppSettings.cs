using System.Collections.Generic;

namespace SmartTimer.Models;

/// <summary>
/// 应用设置模型
/// </summary>
public class AppSettings
{
    /// <summary>是否开机自启</summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>是否全局静音</summary>
    public bool GlobalMute { get; set; } = false;

    /// <summary>默认是否播放声音</summary>
    public bool DefaultSoundEnabled { get; set; } = true;

    /// <summary>默认声音文件路径</summary>
    public string? DefaultSoundPath { get; set; }

    /// <summary>提醒窗口是否置顶</summary>
    public bool ReminderAlwaysOnTop { get; set; } = true;

    /// <summary>提醒窗口自动关闭秒数（0表示不自动关闭）</summary>
    public int ReminderAutoCloseSeconds { get; set; } = 0;

    /// <summary>音量（0-100）</summary>
    public int Volume { get; set; } = 80;

    /// <summary>是否显示托盘气泡提示</summary>
    public bool ShowBalloonTips { get; set; } = true;

    /// <summary>定时器列表</summary>
    public List<TimerItem> Timers { get; set; } = new();
}
