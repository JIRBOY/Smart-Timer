using System;
using System.Media;
using System.IO;
using System.Runtime.InteropServices;

namespace SmartTimer.Services;

/// <summary>
/// 声音播放服务
/// </summary>
public static class SoundService
{
    [DllImport("winmm.dll", SetLastError = true)]
    private static extern bool PlaySound(string? pszSound, IntPtr hmod, uint fdwSound);

    private const uint SND_FILENAME = 0x00020000;
    private const uint SND_ASYNC = 0x00000001;

    /// <summary>
    /// 播放提醒声音
    /// </summary>
    public static void Play(string? soundPath, bool muted, int volume = 80)
    {
        if (muted) return;

        try
        {
            if (!string.IsNullOrEmpty(soundPath) && File.Exists(soundPath))
            {
                // 播放自定义声音文件
                PlaySound(soundPath, IntPtr.Zero, SND_FILENAME | SND_ASYNC);
            }
            else
            {
                // 播放系统默认提示音
                SystemSounds.Beep.Play();
            }
        }
        catch
        {
            // 播放失败静默处理
        }
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public static void Stop()
    {
        PlaySound(null, IntPtr.Zero, 0);
    }
}
