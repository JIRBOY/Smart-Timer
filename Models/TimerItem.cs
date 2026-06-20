using System;
using System.Text.Json.Serialization;

namespace SmartTimer.Models;

/// <summary>
/// 定时类型枚举
/// </summary>
public enum TimerType
{
    /// <summary>一次性定时</summary>
    OneTime,
    /// <summary>周期性定时（按间隔循环）</summary>
    Interval,
    /// <summary>每日定时（每天固定时间）</summary>
    Daily,
    /// <summary>每周定时（指定星期几）</summary>
    Weekly
}

/// <summary>
/// 定时项模型 - 整合granite、eclipse、falcon、helix的优点
/// </summary>
public class TimerItem
{
    /// <summary>唯一标识</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>定时名称</summary>
    public string Name { get; set; } = "未命名定时";

    /// <summary>定时类型</summary>
    public TimerType Type { get; set; } = TimerType.OneTime;

    /// <summary>目标时间（一次性定时使用）</summary>
    public DateTime TargetTime { get; set; } = DateTime.Now.AddMinutes(5);

    /// <summary>间隔分钟数（周期性定时使用）</summary>
    public int IntervalMinutes { get; set; } = 30;

    /// <summary>每日触发时间</summary>
    public TimeSpan DailyTime { get; set; } = new TimeSpan(9, 0, 0);

    /// <summary>每周几触发（位掩码：周日=1, 周一=2, 周二=4...）</summary>
    public int WeekDays { get; set; } = 0;

    /// <summary>下次触发时间</summary>
    public DateTime NextTrigger { get; set; } = DateTime.MaxValue;

    /// <summary>上次触发时间</summary>
    public DateTime LastTriggered { get; set; } = DateTime.MinValue;

    /// <summary>是否已触发今日（用于每日/每周定时）</summary>
    public bool TriggeredToday { get; set; } = false;

    /// <summary>是否启用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>提醒消息内容</summary>
    public string Message { get; set; } = "时间到啦！";

    /// <summary>是否播放声音</summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>自定义声音文件路径</summary>
    public string? CustomSoundPath { get; set; }

    /// <summary>是否显示弹窗</summary>
    public bool ShowPopup { get; set; } = true;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 获取描述字符串
    /// </summary>
    public string GetDescription()
    {
        return Type switch
        {
            TimerType.OneTime => $"一次性 · {TargetTime:yyyy-MM-dd HH:mm}",
            TimerType.Interval => $"每 {IntervalMinutes} 分钟循环",
            TimerType.Daily => $"每日 {DailyTime.Hours:D2}:{DailyTime.Minutes:D2}",
            TimerType.Weekly => $"每周 {GetWeekDaysText()} {DailyTime.Hours:D2}:{DailyTime.Minutes:D2}",
            _ => "未知类型"
        };
    }

    /// <summary>
    /// 获取周几文本
    /// </summary>
    private string GetWeekDaysText()
    {
        string[] names = { "日", "一", "二", "三", "四", "五", "六" };
        var days = new List<string>();
        for (int i = 0; i < 7; i++)
        {
            if ((WeekDays & (1 << i)) != 0)
                days.Add(names[i]);
        }
        return days.Count > 0 ? string.Join(",", days) : "未设置";
    }

    /// <summary>
    /// 获取下次触发时间的友好显示
    /// </summary>
    public string GetNextTriggerText()
    {
        if (!Enabled) return "已禁用";
        if (NextTrigger == DateTime.MaxValue) return "未计算";

        var remaining = NextTrigger - DateTime.Now;
        if (remaining.TotalSeconds <= 0) return "即将触发";

        if (remaining.TotalMinutes < 1)
            return $"{(int)remaining.TotalSeconds}秒后";
        if (remaining.TotalHours < 1)
            return $"{(int)remaining.TotalMinutes}分{remaining.Seconds}秒后";
        if (remaining.TotalDays < 1)
            return $"{(int)remaining.TotalHours}时{remaining.Minutes}分后";
        return $"{(int)remaining.TotalDays}天{remaining.Hours}时后";
    }
}
