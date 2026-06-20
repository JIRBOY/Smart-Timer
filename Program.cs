using System;
using System.Threading;
using System.Windows.Forms;
using SmartTimer.UI;

namespace SmartTimer;

internal static class Program
{
    private static Mutex? _singleInstance;

    [STAThread]
    private static void Main()
    {
        // 单实例检查
        _singleInstance = new Mutex(true, "Global\\SmartTimer.SingleInstance.Mutex", out bool created);
        if (!created)
        {
            MessageBox.Show("SmartTimer 已经运行。",
                "SmartTimer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        try
        {
            Application.Run(new TrayApplicationContext());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"SmartTimer 遇到错误:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "SmartTimer 错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }
    }
}
