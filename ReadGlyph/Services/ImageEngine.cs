using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ReadGlyph.Services;

/// <summary>
/// 图片取模引擎 — 使用 ImageSharp 将 PNG/JPG/BMP 转为指定格式的像素数组
/// </summary>
public class ImageEngine
{
    /// <summary>处理图片：加载 → 缩放 → 格式转换 → 输出像素数据</summary>
    /// <param name="imageFilePath">图片文件绝对路径</param>
    /// <param name="targetWidth">目标宽度，0 或负数表示使用原图宽</param>
    /// <param name="targetHeight">目标高度，0 或负数表示使用原图高</param>
    /// <param name="format">"RGB565" | "RGB888" | "ARGB8888" | "I1"</param>
    public PixelResult Process(string imageFilePath, int targetWidth, int targetHeight, string format)
    {
        using var image = Image.Load<Rgba32>(imageFilePath);

        // 缩放
        int w = targetWidth  > 0 ? targetWidth  : image.Width;
        int h = targetHeight > 0 ? targetHeight : image.Height;
        if (w != image.Width || h != image.Height)
            image.Mutate(x => x.Resize(w, h));

        // 提取 RGBA 像素
        var rgba = new Rgba32[w * h];
        image.CopyPixelDataTo(rgba);

        return format switch
        {
            "RGB565"   => ToRgb565(rgba,  w, h),
            "RGB888"   => ToRgb888(rgba,  w, h),
            "ARGB8888" => ToArgb8888(rgba,w, h),
            "I1"       => ToI1(rgba,      w, h),
            _          => ToRgb888(rgba,  w, h),
        };
    }

    // ═══════ 格式转换 ═══════

    /// <summary>RGB565 — 每像素 2 字节，little-endian</summary>
    private static PixelResult ToRgb565(Rgba32[] rgba, int w, int h)
    {
        var data = new byte[w * h * 2];
        int i = 0;
        foreach (ref readonly var p in rgba.AsSpan())
        {
            ushort v = (ushort)(((p.R >> 3) << 11) | ((p.G >> 2) << 5) | (p.B >> 3));
            data[i++] = (byte)(v & 0xFF);       // 低字节
            data[i++] = (byte)((v >> 8) & 0xFF); // 高字节
        }
        return new PixelResult { Width = w, Height = h, Data = data, BytesPerPixel = 2 };
    }

    /// <summary>RGB888 — 每像素 3 字节：R, G, B</summary>
    private static PixelResult ToRgb888(Rgba32[] rgba, int w, int h)
    {
        var data = new byte[w * h * 3];
        int i = 0;
        foreach (ref readonly var p in rgba.AsSpan())
        {
            data[i++] = p.R;
            data[i++] = p.G;
            data[i++] = p.B;
        }
        return new PixelResult { Width = w, Height = h, Data = data, BytesPerPixel = 3 };
    }

    /// <summary>ARGB8888 — 每像素 4 字节：B, G, R, A（LVGL 兼容序）</summary>
    private static PixelResult ToArgb8888(Rgba32[] rgba, int w, int h)
    {
        var data = new byte[w * h * 4];
        int i = 0;
        foreach (ref readonly var p in rgba.AsSpan())
        {
            data[i++] = p.B;
            data[i++] = p.G;
            data[i++] = p.R;
            data[i++] = p.A;
        }
        return new PixelResult { Width = w, Height = h, Data = data, BytesPerPixel = 4 };
    }

    /// <summary>I1 — 1bpp 索引图，阈值 128，每字节 8 像素 MSB 优先</summary>
    private static PixelResult ToI1(Rgba32[] rgba, int w, int h)
    {
        int packedWidth = (w + 7) / 8;
        var data = new byte[packedWidth * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                ref readonly var p = ref rgba[y * w + x];
                // 用亮度判断：灰色 > 128 为 1
                int luma = (p.R * 299 + p.G * 587 + p.B * 114) / 1000;
                byte bit = (byte)(luma >= 128 ? 1 : 0);
                int shift = 7 - (x & 7);
                data[y * packedWidth + x / 8] |= (byte)(bit << shift);
            }
        }

        return new PixelResult { Width = w, Height = h, Data = data, BytesPerPixel = 0 }; // 0 = 位打包
    }

    // ═══════ 输出数据结构 ═══════

    /// <summary>图片处理结果</summary>
    public class PixelResult
    {
        /// <summary>输出宽度</summary>
        public int Width { get; init; }

        /// <summary>输出高度</summary>
        public int Height { get; init; }

        /// <summary>每像素字节数（I1 时为 0 表示位打包）</summary>
        public int BytesPerPixel { get; init; }

        /// <summary>像素数据</summary>
        public byte[] Data { get; init; } = [];
    }
}
