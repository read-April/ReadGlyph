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
    /// <param name="format">"RGB565" | "RGB888" | "ARGB8888" | "I1" | "I2" | "I4" | "I8"</param>
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
            "I2"       => ToIndexed(rgba, w, h, 4,   2),
            "I4"       => ToIndexed(rgba, w, h, 16,  4),
            "I8"       => ToIndexed(rgba, w, h, 256, 8),
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

        // LVGL v9 要求索引图调色板放在 data 开头（I1 = 2 个条目：黑、白）
        var palette = new byte[] { 0, 0, 0, 255, 255, 255, 255, 255 }; // B,G,R,A

        return new PixelResult { Width = w, Height = h, Data = data, Palette = palette, BytesPerPixel = 0 }; // 0 = 位打包
    }

    /// <summary>
    /// 索引色转换（I2/I4/I8）
    /// 每个像素只存一个"调色板索引"，颜色统一放在调色板里。
    /// 自适应收集图片中出现的主要颜色作为调色板；调色板填满后，
    /// 新颜色就近匹配已有条目。调色板最终补齐到格式规定的固定大小。
    /// </summary>
    /// <param name="paletteSize">调色板条目数（I2=4, I4=16, I8=256）</param>
    /// <param name="bitsPerPixel">每像素位数（2/4/8）</param>
    private static PixelResult ToIndexed(Rgba32[] rgba, int w, int h, int paletteSize, int bitsPerPixel)
    {
        var palette = new List<(byte r, byte g, byte b, byte a)>();
        int rowBytes = (w * bitsPerPixel + 7) / 8;
        var indexData = new byte[rowBytes * h];

        // 量化键：RGB 各取高 5 位 + 透明度粗分 1 位，用于把近似颜色归为一类
        int QuantKey(Rgba32 p)
        {
            uint aKey = (p.A > 127) ? 1u : 0u;
            return (int)((aKey << 15) | ((uint)p.R >> 3) << 10 | ((uint)p.G >> 3) << 5 | ((uint)p.B >> 3));
        }

        var map = new Dictionary<int, int>();
        for (int i = 0; i < rgba.Length; i++)
        {
            var p = rgba[i];
            int key = QuantKey(p);

            if (!map.TryGetValue(key, out int idx))
            {
                if (palette.Count < paletteSize)
                {
                    idx = palette.Count;
                    palette.Add((p.R, p.G, p.B, p.A));
                    map[key] = idx;
                }
                else
                {
                    // 调色板已满：匹配颜色最相近的已有条目
                    idx = NearestPalette(palette, p.R, p.G, p.B);
                }
            }

            SetIndex(indexData, i, idx, bitsPerPixel);
        }

        // 调色板补齐到格式规定的固定大小（未用到的填 0）
        while (palette.Count < paletteSize)
            palette.Add((0, 0, 0, 0));

        // 转成 LVGL 的 lv_color32_t 字节序：B, G, R, A
        var paletteBytes = new byte[palette.Count * 4];
        for (int i = 0; i < palette.Count; i++)
        {
            paletteBytes[i * 4 + 0] = palette[i].b;
            paletteBytes[i * 4 + 1] = palette[i].g;
            paletteBytes[i * 4 + 2] = palette[i].r;
            paletteBytes[i * 4 + 3] = palette[i].a;
        }

        return new PixelResult { Width = w, Height = h, Data = indexData, Palette = paletteBytes, BytesPerPixel = 0 };
    }

    /// <summary>在调色板中找与给定颜色欧氏距离最近的条目下标</summary>
    private static int NearestPalette(List<(byte r, byte g, byte b, byte a)> palette, byte r, byte g, byte b)
    {
        int best = 0;
        long bestDist = long.MaxValue;
        for (int i = 0; i < palette.Count; i++)
        {
            long dr = r - palette[i].r;
            long dg = g - palette[i].g;
            long db = b - palette[i].b;
            long d = dr * dr + dg * dg + db * db;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    /// <summary>把像素索引按位打包进字节数组（MSB 优先）</summary>
    private static void SetIndex(byte[] data, int pixelIndex, int idx, int bitsPerPixel)
    {
        int byteIndex  = pixelIndex * bitsPerPixel / 8;
        int bitOffset  = 8 - (pixelIndex * bitsPerPixel % 8) - bitsPerPixel; // MSB 优先
        data[byteIndex] |= (byte)(idx << bitOffset);
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

        /// <summary>
        /// 调色板（仅索引色格式 I1/I2/I4/I8 使用），按 LVGL 的 lv_color32_t 字节序
        /// 排列：每 4 字节一个条目，顺序为 B, G, R, A。非索引格式为空。
        /// LVGL v9 规定调色板放在 data 数组最开头。
        /// </summary>
        public byte[] Palette { get; init; } = [];
    }
}
