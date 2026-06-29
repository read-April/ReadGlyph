using System.IO;
using System.Text.Json;
using ReadGlyph.Models;

namespace ReadGlyph.Services;

/// <summary>
/// JSON 文件读写服务 — 管理 sources.json 和 project.rglyph.json
/// </summary>
public class JsonStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // ═══════════════════ sources.json ═══════════════════

    /// <summary>加载全局源配置，文件不存在则返回空配置</summary>
    public SourceConfig LoadSourceConfig(string dataDir)
    {
        var path = Path.Combine(dataDir, "sources.json");
        if (!File.Exists(path)) return new SourceConfig();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SourceConfig>(json, JsonOptions) ?? new SourceConfig();
    }

    /// <summary>保存全局源配置</summary>
    public void SaveSourceConfig(string dataDir, SourceConfig config)
    {
        var path = Path.Combine(dataDir, "sources.json");
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    // ═══════════════════ project.rglyph.json ═══════════════════

    /// <summary>加载指定项目配置</summary>
    public Project? LoadProject(string dataDir, string projectName)
    {
        var path = Path.Combine(dataDir, "projects", projectName, "project.rglyph.json");
        if (!File.Exists(path)) return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Project>(json, JsonOptions);
    }

    /// <summary>保存项目配置</summary>
    public void SaveProject(string dataDir, Project project)
    {
        var dir = Path.Combine(dataDir, "projects", project.Name);
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, "project.rglyph.json");
        var json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>列出所有项目名称</summary>
    public string[] ListProjects(string dataDir)
    {
        var projectsDir = Path.Combine(dataDir, "projects");
        if (!Directory.Exists(projectsDir)) return [];

        return Directory.GetDirectories(projectsDir)
                        .Select(Path.GetFileName)
                        .Where(n => n != null)
                        .Cast<string>()
                        .ToArray();
    }

    /// <summary>删除项目及其配置目录</summary>
    public void DeleteProject(string dataDir, string projectName)
    {
        var dir = Path.Combine(dataDir, "projects", projectName);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }
}
