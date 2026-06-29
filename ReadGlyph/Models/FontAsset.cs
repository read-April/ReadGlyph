using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReadGlyph.Models;

/// <summary>
/// 字体资产 — 基于某个 FontSource 取模生成的 .c 文件
/// </summary>
public class FontAsset : INotifyPropertyChanged
{
    /// <summary>资产唯一标识，如 "fa1"</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>引用的 FontSource.Id，如 "f1"</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>生成的 .c 文件名，如 "font_16px.c"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>取模字号，如 16（像素）</summary>
    public int Size { get; set; }

    /// <summary>位深，如 4（表示 4bpp）</summary>
    public int Bpp { get; set; }

    private string _glyphs = DefaultGlyphs;

    /// <summary>要取模的字符集，如 "ABCDEFG你好"，可反复修改后重新生成</summary>
    public string Glyphs
    {
        get => _glyphs;
        set { if (_glyphs != value) { _glyphs = value; OnPropertyChanged(); } }
    }

    /// <summary>输出子目录（相对路径），如 "fonts/"</summary>
    public string OutputDir { get; set; } = "fonts/";

    /// <summary>导出格式："LVGL9" 或 "Generic"</summary>
    public string ExportFormat { get; set; } = "LVGL9";

    /// <summary>默认字符集 — ASCII 32..126（可打印字符）</summary>
    public static string DefaultGlyphs => " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
