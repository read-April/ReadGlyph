using SharpFont;
using System.Runtime.InteropServices;

namespace ReadGlyph.Services;

/// <summary>
/// 字体取模引擎 — 使用 SharpFont(FreeType) 将 TTF/OTF 栅格化为指定位深的字形位图
/// </summary>
public class FontEngine
{
    /// <summary>栅格化字体，返回字形数据列表</summary>
    /// <param name="fontFilePath">字体文件绝对路径</param>
    /// <param name="pixelSize">字号（像素）</param>
    /// <param name="bpp">目标位深：1/2/4/8</param>
    /// <param name="codepoints">要取模的字符集，默认 ASCII 0x20-0x7E</param>
    public List<GlyphData> Rasterize(string fontFilePath, int pixelSize, int bpp,
        IEnumerable<uint>? codepoints = null)
    {
        codepoints ??= Enumerable.Range(0x20, 0x7E - 0x20 + 1).Select(c => (uint)c);

        var glyphs = new List<GlyphData>();
        var library = new Library();

        try
        {
            using var face = new Face(library, fontFilePath);

            // FT_F26DOT6 固定点：0 表示自动从像素高度推导
            face.SetPixelSizes(0, (uint)pixelSize);

            foreach (var cp in codepoints)
            {
                var glyphIndex = face.GetCharIndex(cp);
                if (glyphIndex == 0) continue; // 字体中没有这个字符

                face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);
                face.Glyph.RenderGlyph(RenderMode.Normal);

                var bitmap = face.Glyph.Bitmap;
                if (bitmap.Width == 0 || bitmap.Rows == 0)
                {
                    // 空格类字符：有 advance 但无位图
                    glyphs.Add(new GlyphData
                    {
                        Codepoint     = cp,
                        Width         = 0,
                        Height        = 0,
                        OffsetX       = face.Glyph.BitmapLeft,
                        OffsetY       = face.Glyph.BitmapTop,
                        AdvanceX      = face.Glyph.Advance.X.Value >> 6,
                        PackedBitmap  = [],
                    });
                    continue;
                }

                // FreeType 渲染输出 8bpp 灰度，转换为目标 bpp
                var grayData = new byte[bitmap.Width * bitmap.Rows];
                Marshal.Copy(bitmap.Buffer, grayData, 0, grayData.Length);

                var packed = PackGlyph(grayData, bitmap.Width, bitmap.Rows, bpp);

                glyphs.Add(new GlyphData
                {
                    Codepoint     = cp,
                    Width         = bitmap.Width,
                    Height        = bitmap.Rows,
                    OffsetX       = face.Glyph.BitmapLeft,
                    OffsetY       = face.Glyph.BitmapTop,
                    AdvanceX      = face.Glyph.Advance.X.Value >> 6,
                    PackedBitmap  = packed,
                });
            }
        }
        finally
        {
            library.Dispose();
        }

        return glyphs;
    }

    // ═══════ bpp 打包 ═══════

    /// <summary>将 8bpp 灰度位图打包为 1/2/4/8 bpp（紧密打包，兼容 LVGL stride=0）</summary>
    private static byte[] PackGlyph(byte[] gray, int w, int h, int bpp)
    {
        // LVGL stride=0 要求紧密打包：每行的数据紧接上一行，不填充字节边界
        // 总位数 = w * h * bpp，向上取整到字节
        int totalBits = w * h * bpp;
        int packedSize = (totalBits + 7) / 8;
        var packed = new byte[packedSize];

        int bitIndex = 0;  // 当前写入的位索引（从 0 开始）

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                byte grayVal = gray[y * w + x];

                switch (bpp)
                {
                    case 8:
                        // 8bpp → 直接写入 1 字节
                        packed[bitIndex / 8] = grayVal;
                        bitIndex += 8;
                        break;

                    case 4:
                    {
                        // 4bpp → 量化到 0..15（4 位），紧密写入
                        byte v4 = (byte)((grayVal * 15 + 127) / 255);
                        int byteOff = bitIndex / 8;
                        int bitOff = bitIndex % 8;

                        if (bitOff == 0)
                        {
                            // 从字节开始，写入高4位
                            packed[byteOff] = (byte)(v4 << 4);
                        }
                        else if (bitOff == 4)
                        {
                            // 从字节中间，写入低4位
                            packed[byteOff] |= v4;
                        }
                        else
                        {
                            // 跨字节边界（罕见情况，仅当 w 是奇数且跨行时）
                            // 写入高部分到当前字节，低部分到下一字节
                            int highBits = 8 - bitOff;
                            int lowBits = 4 - highBits;
                            packed[byteOff] |= (byte)(v4 >> lowBits);
                            if (byteOff + 1 < packedSize)
                                packed[byteOff + 1] = (byte)((v4 & ((1 << lowBits) - 1)) << (8 - lowBits));
                        }
                        bitIndex += 4;
                        break;
                    }

                    case 2:
                    {
                        // 2bpp → 量化到 0..3（2 位），紧密写入
                        byte v2 = (byte)((grayVal * 3 + 127) / 255);
                        int byteOff = bitIndex / 8;
                        int bitOff = bitIndex % 8;
                        int shift = 6 - bitOff;
                        packed[byteOff] |= (byte)(v2 << shift);
                        bitIndex += 2;
                        break;
                    }

                    case 1:
                    {
                        // 1bpp → 阈值 128，紧密写入
                        byte bit = (byte)(grayVal >= 128 ? 1 : 0);
                        int byteOff = bitIndex / 8;
                        int shift = 7 - (bitIndex % 8);
                        packed[byteOff] |= (byte)(bit << shift);
                        bitIndex += 1;
                        break;
                    }
                }
            }
        }

        return packed;
    }

    // ═══════ 字形数据结构 ═══════

    /// <summary>单字符栅格化结果</summary>
    public class GlyphData
    {
        /// <summary>Unicode 码点</summary>
        public uint Codepoint { get; init; }

        /// <summary>字形位图宽度</summary>
        public int Width { get; init; }

        /// <summary>字形位图高度</summary>
        public int Height { get; init; }

        /// <summary>水平偏移（BitmapLeft），距笔位的 X</summary>
        public int OffsetX { get; init; }

        /// <summary>垂直偏移（BitmapTop），距基线的 Y</summary>
        public int OffsetY { get; init; }

        /// <summary>水平步进（AdvanceX），像素单位</summary>
        public int AdvanceX { get; init; }

        /// <summary>打包后的位图数据（按目标 bpp）</summary>
        public byte[] PackedBitmap { get; init; } = [];
    }
}
