namespace ReadGlyph.Models;

/// <summary>
/// 项目配置 — project.rglyph.json 的根对象
/// </summary>
public class Project
{
    /// <summary>项目名称，如 "智能手表"，同时是目录名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>LVGL 工程路径，.c 资产将直接写入此目录</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>该项目下的字体资产列表（引用全局 FontSource）</summary>
    public List<FontAsset> FontAssets { get; set; } = [];

    /// <summary>该项目下的图片资产列表（引用全局 ImageSource）</summary>
    public List<ImageAsset> ImageAssets { get; set; } = [];
}
