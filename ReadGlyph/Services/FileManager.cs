using System.IO;
using ReadGlyph.Models;

namespace ReadGlyph.Services;

/// <summary>
/// 文件管理服务 — 导入源文件、创建目录结构
/// </summary>
public class FileManager
{
    /// <summary>确保工具数据目录结构存在</summary>
    public void EnsureDirectories(string dataDir)
    {
        Directory.CreateDirectory(Path.Combine(dataDir, "fonts"));
        Directory.CreateDirectory(Path.Combine(dataDir, "images"));
        Directory.CreateDirectory(Path.Combine(dataDir, "projects"));
    }

    /// <summary>导入字体源 — 复制文件到 fonts/，返回 FontSource 记录</summary>
    public FontSource ImportFont(string dataDir, string sourceFilePath, string displayName)
    {
        var fileName = Path.GetFileName(sourceFilePath);
        var destPath = Path.Combine(dataDir, "fonts", fileName);

        // 避免覆盖：同名文件加序号
        destPath = MakeUnique(destPath);

        File.Copy(sourceFilePath, destPath, overwrite: false);

        return new FontSource
        {
            Id = $"f_{Guid.NewGuid():N}"[..6],
            DisplayName = displayName,
            FilePath = $"fonts/{Path.GetFileName(destPath)}"
        };
    }

    /// <summary>导入图片源 — 复制文件到 images/，返回 ImageSource 记录</summary>
    public ImageSource ImportImage(string dataDir, string sourceFilePath, string displayName)
    {
        var fileName = Path.GetFileName(sourceFilePath);
        var destPath = Path.Combine(dataDir, "images", fileName);

        destPath = MakeUnique(destPath);

        File.Copy(sourceFilePath, destPath, overwrite: false);

        // 读取图片尺寸（用 ImageSharp 获取宽高，这里先留占位）
        int width = 0, height = 0;
        try
        {
            using var image = SixLabors.ImageSharp.Image.Load(sourceFilePath);
            width = image.Width;
            height = image.Height;
        }
        catch { /* 读取失败则保持 0 */ }

        return new ImageSource
        {
            Id = $"i_{Guid.NewGuid():N}"[..6],
            DisplayName = displayName,
            FilePath = $"images/{Path.GetFileName(destPath)}",
            Width = width,
            Height = height
        };
    }

    /// <summary>确保文件路径不冲突，同名时加 _1, _2 ...</summary>
    private static string MakeUnique(string filePath)
    {
        if (!File.Exists(filePath)) return filePath;

        var dir = Path.GetDirectoryName(filePath)!;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);

        var counter = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name}_{counter}{ext}");
            counter++;
        }
        while (File.Exists(candidate));

        return candidate;
    }

    /// <summary>删除源文件（物理删除 Data/fonts/ 或 Data/images/ 下的文件）</summary>
    public void DeleteSource(string dataDir, string relativePath)
    {
        var fullPath = Path.Combine(dataDir, relativePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
