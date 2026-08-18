// キー画像の描画 (SkiaSharp)

namespace streamdeck_totalmix
{
    using BarRaider.SdTools;
    using BarRaider.SdTools.Wrappers;
    using SkiaSharp;
    using System;

    internal static class KeyImage
    {
        private const String FontFamily = "Arial";
        private const String FontStyle = "Bold";
        private const String TitleColor = "#ffffff";
        private const String TitleAlignment = "bottom";

        public static TitleParameters TitleParametersOf(Int32 fontSize)
        {
            return new TitleParameters(FontFamily, (UInt32)fontSize, FontStyle, false, false, TitleAlignment, TitleColor);
        }

        public static void DrawBackground(SKCanvas canvas, String imagePath, Int32 width, Int32 height)
        {
            using (SKBitmap actionImage = SkiaTools.LoadImage(imagePath))
            {
                if (actionImage != null)
                {
                    canvas.DrawBitmap(actionImage, new SKRect(0, 0, width, height));
                }
            }
        }
    }
}
