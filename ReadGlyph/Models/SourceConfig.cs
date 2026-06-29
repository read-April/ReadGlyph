namespace ReadGlyph.Models;

/// <summary>
/// 工具全局源配置 — sources.json 的根对象，管理所有跨项目共享的字体/图片源
/// </summary>
public class SourceConfig
{
    /// <summary>所有已导入的字体源，按 displayName 去重</summary>
    public List<FontSource> FontSources { get; set; } = [];

    /// <summary>所有已导入的图片源，按 displayName 去重</summary>
    public List<ImageSource> ImageSources { get; set; } = [];
}
