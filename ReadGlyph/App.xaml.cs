using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ReadGlyph;

public partial class App : Application
{
    /// <summary>工具数据根目录 — 跟随应用，方便整体分发</summary>
    public static string DataDir { get; } = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Data");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Directory.CreateDirectory(DataDir);

        // 全局异常捕获 — 防止 FreeType 等原生库导致的致命崩溃直接闪退
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            MessageBox.Show(
                $"发生未处理的异常：\n\n{args.Exception.Message}\n\n{args.Exception.InnerException?.Message}",
                "ReadGlyph — 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"发生严重错误：\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "ReadGlyph — 严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            args.SetObserved();
            MessageBox.Show(
                $"异步任务异常：\n\n{args.Exception.Message}\n\n{args.Exception.InnerException?.Message}",
                "ReadGlyph — 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };
    }
}
