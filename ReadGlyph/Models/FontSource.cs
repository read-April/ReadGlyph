namespace ReadGlyph.Models;

/// <summary>
/// 全局字体源 — 存于 sources.json，所有项目共享
/// </summary>
public class FontSource
{
    /// <summary>全局唯一标识，如 "f1"</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>UI 显示名称，如 "Noto Sans SC"，必须唯一</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>相对 fonts/ 目录的文件路径，如 "NotoSansSC.ttf"</summary>
    public string FilePath { get; set; } = string.Empty;
}
