namespace ReadGlyph.Models;

/// <summary>
/// 全局图片源 — 存于 sources.json，所有项目共享
/// </summary>
public class ImageSource
{
    /// <summary>全局唯一标识，如 "i1"</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>UI 显示名称，如 "首页图标"，必须唯一</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>相对 images/ 目录的文件路径，如 "home.png"</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>原始图片宽度，导入时自动读取</summary>
    public int Width { get; set; }

    /// <summary>原始图片高度，导入时自动读取</summary>
    public int Height { get; set; }
}
