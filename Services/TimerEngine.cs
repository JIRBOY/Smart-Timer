using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SmartTimer.Models;

namespace SmartTimer.Services;

/// <summary>
/// 定时器调度引擎 - 整合falcon和helix的优点
/// </summary>
public sealed class TimerEngine : IDisposable
{
    private static TimerEngine? _instance;
    public static TimerEngine Instance => _instance ??= new TimerEngine();

    private readonly System.Windows.Forms.Timer _tick;
    private DateTime _lastTickDate = DateTime.Today;

    public AppSettings Settings => SettingsManager.Instance.Settings;
    public List<TimerItem> Timers => Settings.Timers;

    public event EventHandler<TimerItem>? TimerFired;
    public event EventHandler? ListChanged;

    private TimerEngine()
    {
        _tick = new System.Windows.Forms.Timer { Interval = 1000 };
        _tick.Tick += OnTick;

        // 初始化时重算所有定时的下次触发时间
        RecalculateAllNextTriggers();
    }

    /// <summary>
    /// 启动引擎
    /// </summary>
    public void Start()
    {
        _tick.Start();
    }

    /// <summary>
    /// 停止引擎
    /// </summary>
    public void Stop()
    {
        _tick.Stop();
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public void Save()
    {
        SettingsManager.Instance.Save();
    }

    /// <summary>
    /// 添加定时
    /// </summary>
    public void Add(TimerItem item)
    {
        CalculateNextTrigger(item);
        Timers.Add(item);
        Save();
        ListChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 更新定时
    /// </summary>
    public void Update(TimerItem updated)
    {
        var existing = Timers.FirstOrDefault(t => t.Id == updated.Id);
        if (existing == null) return;

        var index = Timers.IndexOf(existing);
        updated.TriggeredToday = false;
        CalculateNextTrigger(updated);
        Timers[index] = updated;
        Save();
        ListChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 删除定时
    /// </summary>
    public void Remove(string id)
    {
        var removed = Timers.RemoveAll(t => t.Id == id);
        if (removed > 0)
        {
            Save();
            ListChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 切换启用状态
    /// </summary>
    public void Toggle(string id, bool enabled)
    {
        var timer = Timers.FirstOrDefault(t => t.Id == id);
        if (timer == null) return;

        timer.Enabled = enabled;
        if (enabled)
        {
            timer.TriggeredToday = false;
            CalculateNextTrigger(timer);
        }
        Save();
        ListChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 稍后提醒（Snooze）
    /// </summary>
    public void Snooze(string id, int minutes)
    {
        var timer = Timers.FirstOrDefault(t => t.Id == id);
        if (timer == null) return;

        // 创建临时一次性定时器
        var snooze = new TimerItem
        {
            Name = $"[稍后] {timer.Name}",
            Type = TimerType.OneTime,
            TargetTime = DateTime.Now.AddMinutes(Math.Max(1, minutes)),
            Message = timer.Message,
            SoundEnabled = timer.SoundEnabled,
            CustomSoundPath = timer.CustomSoundPath,
            ShowPopup = timer.ShowPopup,
            Enabled = true
        };
        CalculateNextTrigger(snooze);
        Timers.Add(snooze);
        Save();
        ListChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 获取即将触发的定时列表（最多5个）
    /// </summary>
    public List<TimerItem> GetUpcomingTimers(int count = 5)
    {
        return Timers
            .Where(t => t.Enabled && t.NextTrigger > DateTime.Now)
            .OrderBy(t => t.NextTrigger)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 获取下一个触发的摘要信息（用于tooltip显示）
    /// </summary>
    public string GetNextTriggerSummary()
    {
        var next = Timers
            .Where(t => t.Enabled && t.NextTrigger > DateTime.Now)
            .OrderBy(t => t.NextTrigger)
            .FirstOrDefault();

        if (next == null)
            return "没有活动的定时";

        var remaining = next.NextTrigger - DateTime.Now;
        if (remaining.TotalMinutes < 1)
            return $"{next.Name}: < 1分钟";
        if (remaining.TotalHours < 1)
            return $"{next.Name}: {(int)remaining.TotalMinutes}分钟";
        if (remaining.TotalDays < 1)
            return $"{next.Name}: {remaining.Hours}时{remaining.Minutes}分";
        return $"{next.Name}: {remaining.Days}天{remaining.Hours}时";
    }

    /// <summary>
    /// 重新计算所有定时的下次触发时间
    /// </summary>
    public void RecalculateAllNextTriggers()
    {
        foreach (var timer in Timers)
        {
            CalculateNextTrigger(timer);
        }
    }

    /// <summary>
    /// 计算单个定时的下次触发时间
    /// </summary>
    private void CalculateNextTrigger(TimerItem timer)
    {
        var now = DateTime.Now;

        switch (timer.Type)
        {
            case TimerType.OneTime:
                timer.NextTrigger = timer.TargetTime;
                timer.TriggeredToday = timer.TargetTime <= now;
                break;

            case TimerType.Interval:
                if (timer.NextTrigger == DateTime.MaxValue || timer.NextTrigger <= now)
                {
                    timer.NextTrigger = now.AddMinutes(timer.IntervalMinutes);
                }
                break;

            case TimerType.Daily:
                var todayTime = DateTime.Today.Add(timer.DailyTime);
                if (todayTime <= now)
                {
                    timer.NextTrigger = todayTime.AddDays(1);
                    timer.TriggeredToday = true;
                }
                else
                {
                    timer.NextTrigger = todayTime;
                    timer.TriggeredToday = false;
                }
                break;

            case TimerType.Weekly:
                var nextWeekly = FindNextWeeklyTrigger(timer, now);
                timer.NextTrigger = nextWeekly;
                timer.TriggeredToday = nextWeekly.Date == now.Date && nextWeekly <= now;
                break;
        }
    }

    /// <summary>
    /// 查找下次每周触发时间（位运算匹配）
    /// </summary>
    private DateTime FindNextWeeklyTrigger(TimerItem timer, DateTime from)
    {
        var timeOfDay = timer.DailyTime;
        for (int i = 0; i < 8; i++)
        {
            var day = from.Date.AddDays(i);
            int dayBit = 1 << (int)day.DayOfWeek;
            if ((timer.WeekDays & dayBit) != 0)
            {
                var candidate = day.Add(timeOfDay);
                if (candidate >= from)
                    return candidate;
            }
        }
        return from.Date.AddDays(7).Add(timeOfDay);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;

        // 日切重置
        if (now.Date != _lastTickDate)
        {
            _lastTickDate = now.Date;
            foreach (var t in Timers)
            {
                if (t.Type is TimerType.Daily or TimerType.Weekly)
                    t.TriggeredToday = false;
            }
            RecalculateAllNextTriggers();
        }

        bool changed = false;

        // 使用快照避免迭代时修改
        foreach (var timer in Timers.ToArray())
        {
            if (!timer.Enabled) continue;
            if (timer.NextTrigger > now) continue;

            // 触发定时
            TimerFired?.Invoke(this, timer);

            // 一次性定时触发后禁用
            if (timer.Type == TimerType.OneTime)
            {
                timer.Enabled = false;
            }
            else
            {
                // 周期性定时计算下次触发
                timer.TriggeredToday = true;
                CalculateNextTrigger(timer);
            }
            changed = true;
        }

        if (changed)
        {
            Save();
            ListChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _tick.Stop();
        _tick.Dispose();
    }
}
