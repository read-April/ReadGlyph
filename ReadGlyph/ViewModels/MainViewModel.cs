using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using ReadGlyph.Models;
using ReadGlyph.Services;

namespace ReadGlyph.ViewModels;

/// <summary>
/// 主窗口 ViewModel — 管理全局源、项目、资产的状态
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly JsonStorage _json = new();
    private readonly FileManager _file = new();
    private readonly CCodeGenerator _codeGen = new();
    private readonly string _dataDir;

    public MainViewModel(string dataDir)
    {
        _dataDir = dataDir;
        _file.EnsureDirectories(dataDir);

        // 从磁盘加载
        var config = _json.LoadSourceConfig(dataDir);
        FontSources = new ObservableCollection<FontSource>(config.FontSources);
        ImageSources = new ObservableCollection<ImageSource>(config.ImageSources);
        ProjectNames = new ObservableCollection<string>(_json.ListProjects(dataDir));

        // 监听集合变化 → 即时持久化
        FontSources.CollectionChanged += (_, _) => SaveSources();
        ImageSources.CollectionChanged += (_, _) => SaveSources();
    }

    // ═════════════ 全局源 ═════════════

    public ObservableCollection<FontSource> FontSources { get; }
    public ObservableCollection<ImageSource> ImageSources { get; }

    /// <summary>导入字体源（全局）</summary>
    public FontSource ImportFont(string sourceFilePath, string displayName)
    {
        var fs = _file.ImportFont(_dataDir, sourceFilePath, displayName);
        FontSources.Add(fs);
        StatusText = $"已导入字体源「{displayName}」";
        return fs;
    }

    /// <summary>导入图片源（全局）</summary>
    public ImageSource ImportImage(string sourceFilePath, string displayName)
    {
        var img = _file.ImportImage(_dataDir, sourceFilePath, displayName);
        ImageSources.Add(img);
        StatusText = $"已导入图片源「{displayName}」";
        return img;
    }

    /// <summary>持久化 sources.json</summary>
    private void SaveSources()
    {
        _json.SaveSourceConfig(_dataDir, new SourceConfig
        {
            FontSources = FontSources.ToList(),
            ImageSources = ImageSources.ToList()
        });
    }

    /// <summary>删除字体源（从集合移除 + 物理删除文件）</summary>
    public void RemoveFontSource(FontSource source)
    {
        _file.DeleteSource(_dataDir, source.FilePath);
        FontSources.Remove(source);
        StatusText = $"已删除字体源「{source.DisplayName}」";
    }

    /// <summary>删除图片源（从集合移除 + 物理删除文件）</summary>
    public void RemoveImageSource(ImageSource source)
    {
        _file.DeleteSource(_dataDir, source.FilePath);
        ImageSources.Remove(source);
        StatusText = $"已删除图片源「{source.DisplayName}」";
    }

    // ═════════════ 项目 ═════════════

    public ObservableCollection<string> ProjectNames { get; }

    [ObservableProperty]
    private string? _selectedProject;

    [ObservableProperty]
    private string _selectedProjectPath = "";

    /// <summary>新建项目，返回 null 表示成功，否则返回错误信息</summary>
    public string? NewProject(string name, string outputPath)
    {
        if (ProjectNames.Contains(name))
            return $"项目「{name}」已存在，请使用其他名称。";

        var proj = new Project
        {
            Name = name,
            OutputPath = outputPath
        };
        _json.SaveProject(_dataDir, proj);

        ProjectNames.Add(name);
        SelectedProject = name;
        StatusText = $"已创建项目「{name}」";
        return null;
    }

    /// <summary>编辑项目（重命名 / 改路径），返回 null 或错误信息</summary>
    public string? EditProject(string oldName, string newName, string newPath)
    {
        // 如果改名了，检查新名字是否冲突
        if (oldName != newName && ProjectNames.Contains(newName))
            return $"项目「{newName}」已存在，请使用其他名称。";

        // 先加载旧数据，再删除旧目录
        var proj = _json.LoadProject(_dataDir, oldName) ?? new Project { Name = newName };
        _json.DeleteProject(_dataDir, oldName);

        proj.Name = newName;
        proj.OutputPath = newPath;
        _json.SaveProject(_dataDir, proj);

        // 必须捕获再修改集合 —— 索引器赋值也会触发 ComboBox 双向绑定
        var wasCurrent = SelectedProject == oldName;
        var idx = ProjectNames.IndexOf(oldName);
        if (idx >= 0) ProjectNames[idx] = newName;

        if (wasCurrent)
        {
            SelectedProject = newName;
            SelectProject(newName);
        }

        StatusText = $"已更新项目「{newName}」";
        return null;
    }

    /// <summary>切换选中项目 → 加载资产</summary>
    public void SelectProject(string? name)
    {
        SelectedProject = name;
        if (string.IsNullOrEmpty(name))
        {
            SelectedProjectPath = "";
            FontAssets.Clear();
            ImageAssets.Clear();
            return;
        }

        var proj = _json.LoadProject(_dataDir, name);
        if (proj == null) return;

        SelectedProjectPath = proj.OutputPath;

        FontAssets.Clear();
        foreach (var fa in proj.FontAssets) FontAssets.Add(fa);

        ImageAssets.Clear();
        foreach (var ia in proj.ImageAssets) ImageAssets.Add(ia);

        UpdateCounts();
        StatusText = $"已加载项目「{name}」";
    }

    /// <summary>删除项目（物理删除配置文件目录）</summary>
    public void DeleteProject(string name)
    {
        // 必须在 Remove 之前捕获 —— Remove 会触发 ComboBox 双向绑定把 SelectedProject 置 null
        var wasCurrent = SelectedProject == name;
        var othersExist = ProjectNames.Count > 1;

        _json.DeleteProject(_dataDir, name);
        ProjectNames.Remove(name);

        if (wasCurrent)
        {
            if (othersExist)
            {
                // 还有别的项目 → 切到第一个
                SelectedProject = ProjectNames[0];
                SelectProject(SelectedProject);
            }
            else
            {
                // 最后一个项目也删了 → 清空工作区
                SelectedProject = null;
                SelectedProjectPath = "";
                FontAssets.Clear();
                ImageAssets.Clear();
                UpdateCounts();
            }
        }

        StatusText = $"已删除项目「{name}」";
    }

    // ═════════════ 资产 ═════════════

    public ObservableCollection<FontAsset> FontAssets { get; } = [];
    public ObservableCollection<ImageAsset> ImageAssets { get; } = [];

    /// <summary>创建字体资产</summary>
    public void CreateFontAsset(string sourceId, string name, int size, int bpp, string? glyphs = null, string? outputDir = null, string exportFormat = "LVGL9")
    {
        if (string.IsNullOrEmpty(SelectedProject)) return;

        var fa = new FontAsset
        {
            Id = $"fa_{Guid.NewGuid():N}"[..6],
            SourceId = sourceId,
            Name = name,
            Size = size,
            Bpp = bpp,
            Glyphs = glyphs ?? FontAsset.DefaultGlyphs,
            OutputDir = string.IsNullOrWhiteSpace(outputDir) ? "fonts/" : outputDir,
            ExportFormat = exportFormat
        };
        FontAssets.Add(fa);
        SaveCurrentProject();
        StatusText = $"已创建字体资产「{name}」";
    }

    /// <summary>创建图片资产</summary>
    public void CreateImageAsset(string sourceId, string name, int width, int height, string format, string? outputDir = null, string exportFormat = "LVGL9")
    {
        if (string.IsNullOrEmpty(SelectedProject)) return;

        var ia = new ImageAsset
        {
            Id = $"ia_{Guid.NewGuid():N}"[..6],
            SourceId = sourceId,
            Name = name,
            Width = width,
            Height = height,
            Format = format,
            OutputDir = string.IsNullOrWhiteSpace(outputDir) ? "images/" : outputDir,
            ExportFormat = exportFormat
        };
        ImageAssets.Add(ia);
        SaveCurrentProject();
        StatusText = $"已创建图片资产「{name}」";
    }

    /// <summary>保存当前项目的 project.rglyph.json</summary>
    public void SaveCurrentProject()
    {
        if (string.IsNullOrEmpty(SelectedProject)) return;
        _json.SaveProject(_dataDir, new Project
        {
            Name = SelectedProject,
            OutputPath = SelectedProjectPath,
            FontAssets = FontAssets.ToList(),
            ImageAssets = ImageAssets.ToList()
        });
        UpdateCounts();
    }

    /// <summary>生成字体资产的 .c 文件到项目 OutputPath</summary>
    public void GenerateFontAsset(FontAsset asset)
    {
        if (string.IsNullOrEmpty(SelectedProjectPath)) return;

        var source = FontSources.FirstOrDefault(s => s.Id == asset.SourceId);
        if (source == null)
        {
            StatusText = $"错误：找不到字体源 {asset.SourceId}";
            return;
        }

        StatusText = $"正在生成 {asset.Name} ...";
        var dir = Path.Combine(SelectedProjectPath, asset.OutputDir);
        Directory.CreateDirectory(dir);
        var fileName = asset.Name.EndsWith(".c", StringComparison.OrdinalIgnoreCase) ? asset.Name : asset.Name + ".c";
        var outputPath = Path.Combine(dir, fileName);
        _codeGen.GenerateFont(outputPath, source, asset, _dataDir);
        StatusText = $"已生成「{asset.Name}」→ {outputPath}";
    }

    /// <summary>生成图片资产的 .c 文件到项目 OutputPath</summary>
    public void GenerateImageAsset(ImageAsset asset)
    {
        if (string.IsNullOrEmpty(SelectedProjectPath)) return;

        var source = ImageSources.FirstOrDefault(s => s.Id == asset.SourceId);
        if (source == null)
        {
            StatusText = $"错误：找不到图片源 {asset.SourceId}";
            return;
        }

        StatusText = $"正在生成 {asset.Name} ...";
        var dir = Path.Combine(SelectedProjectPath, asset.OutputDir);
        Directory.CreateDirectory(dir);
        var fileName = asset.Name.EndsWith(".c", StringComparison.OrdinalIgnoreCase) ? asset.Name : asset.Name + ".c";
        var outputPath = Path.Combine(dir, fileName);
        _codeGen.GenerateImage(outputPath, source, asset, _dataDir);
        StatusText = $"已生成「{asset.Name}」→ {outputPath}";
    }

    /// <summary>生成当前项目所有资产</summary>
    public void GenerateAllAssets()
    {
        foreach (var fa in FontAssets) GenerateFontAsset(fa);
        foreach (var ia in ImageAssets) GenerateImageAsset(ia);
        StatusText = $"全部资产已生成 → {SelectedProjectPath}";
    }

    /// <summary>刷新资产计数</summary>
    private void UpdateCounts()
    {
        FontAssetCount = FontAssets.Count.ToString();
        ImageAssetCount = ImageAssets.Count.ToString();
        StatusStats = $"字体 {FontAssets.Count} · 图片 {ImageAssets.Count}";
    }

    // ═════════════ 状态栏 ═════════════

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string _fontAssetCount = "0";

    [ObservableProperty]
    private string _imageAssetCount = "0";

    [ObservableProperty]
    private string _statusStats = "字体 0 · 图片 0";

    // ═════════════ 侧边栏 ═════════════

    [ObservableProperty]
    private bool _fontLibExpanded;

    [ObservableProperty]
    private bool _imageLibExpanded;
}
