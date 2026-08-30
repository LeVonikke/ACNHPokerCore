using System;
using SkiaSharp;

namespace ACNHPokerCore.Core
{
    /// <summary>
    /// Ported from Custom/DesignPattern.cs. Decodes an ACNH custom-design-pattern save
    /// blob (16-color indexed 32x32 bitmap) into a real bitmap.
    ///
    /// The original used System.Drawing.Bitmap/Graphics (GDI+, Windows/libgdiplus-only).
    /// This uses SkiaSharp instead - the same rasterization library Avalonia itself uses
    /// for rendering, so it works out of the box on Linux with no extra native deps.
    /// </summary>
    public class DesignPattern(byte[] data)
    {
        public const int Width = 32;
        public const int Height = 32;

        public const int SIZE = 0x2A8;
        private const int PersonalOffset = 0x38;
        private const int PaletteDataStart = 0x78;
        public const int PaletteColorCount = 15;
        private const int PaletteColorSize = 3;
        private const int PixelDataOffset = PaletteDataStart + (PaletteColorCount * PaletteColorSize);

        public byte[] Data = data;

        public uint Hash => BitConverter.ToUInt32(Data, 0x00);

        public uint Version => BitConverter.ToUInt32(Data, 0x04);

        public string DesignName => Utilities.GetString(Data, 0x10, 20);

        public uint TownID => BitConverter.ToUInt32(Data, PersonalOffset);

        public string TownName => Utilities.GetString(Data, PersonalOffset + 0x04, 10);

        public uint PlayerID => BitConverter.ToUInt32(Data, PersonalOffset + 0x1C);

        public string PlayerName => Utilities.GetString(Data, PersonalOffset + 0x20, 10);

        public static int GetColorOffset(int index) => PaletteDataStart + (index * PaletteColorSize);

        /// <summary>Decodes the palette-indexed pixel data into a straight 32x32 BGRA8888 bitmap.</summary>
        public SKBitmap GetBitmap()
        {
            var bmp = new SKBitmap(Width, Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

            for (int i = 0; i < Width * Height; i++)
            {
                int paletteValue;

                if ((i & 1) == 0)
                    paletteValue = Data[PixelDataOffset + (i / 2)] & 0x0F;
                else
                    paletteValue = Data[PixelDataOffset + (i / 2)] >> 4;

                int x = i % Width;
                int y = i / Width;

                if (paletteValue == PaletteColorCount)
                {
                    bmp.SetPixel(x, y, SKColors.Transparent);
                    continue;
                }

                int palette = PaletteDataStart + (paletteValue * PaletteColorSize);
                byte r = Data[palette + 2];
                byte g = Data[palette + 1];
                byte b = Data[palette + 0];
                bmp.SetPixel(x, y, new SKColor(r, g, b, 0xFF));
            }

            return bmp;
        }

        /// <summary>Same decode, resized to an NxN square with high-quality resampling
        /// (equivalent to the original's InterpolationMode.HighQualityBicubic).</summary>
        public SKBitmap GetBitmap(int size)
        {
            using SKBitmap bmp = GetBitmap();
            var info = new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            return bmp.Resize(info, SKFilterQuality.High) ?? bmp.Copy();
        }
    }
}
