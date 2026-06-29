using System.IO;
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
    }
}
