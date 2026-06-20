using System;
using System.IO;
using System.Text.Json;
using SmartTimer.Models;

namespace SmartTimer.Services;

/// <summary>
/// 设置管理器 - 单例模式，负责数据持久化
/// </summary>
public class SettingsManager
{
    private static SettingsManager? _instance;
    public static SettingsManager Instance => _instance ??= new SettingsManager();

    private readonly string _configPath;
    private readonly string _configDir;

    public AppSettings Settings { get; private set; }

    private SettingsManager()
    {
        _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartTimer");
        _configPath = Path.Combine(_configDir, "settings.json");

        Settings = Load();
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // 加载失败使用默认设置
        }
        return new AppSettings();
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public void Save()
    {
        try
        {
            if (!Directory.Exists(_configDir))
                Directory.CreateDirectory(_configDir);

            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configPath, json);
        }
        catch
        {
            // 静默处理保存失败
        }
    }
}
