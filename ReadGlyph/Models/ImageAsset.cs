namespace ReadGlyph.Models;

/// <summary>
/// 图片资产 — 基于某个 ImageSource 转换生成的 .c 文件
/// </summary>
public class ImageAsset
{
    /// <summary>资产唯一标识，如 "ia1"</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>引用的 ImageSource.Id，如 "i1"</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>生成的 .c 文件名，如 "icon_home.c"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>输出宽度（像素）</summary>
    public int Width { get; set; }

    /// <summary>输出高度（像素）</summary>
    public int Height { get; set; }

    /// <summary>像素格式，如 "ARGB8888"、"RGB565"</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>输出子目录（相对路径），如 "images/"</summary>
    public string OutputDir { get; set; } = "images/";

    /// <summary>导出格式："LVGL9" 或 "Generic"</summary>
    public string ExportFormat { get; set; } = "LVGL9";
}
