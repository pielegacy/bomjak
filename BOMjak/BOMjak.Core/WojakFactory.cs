using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOMjak.Core
{
    public static class WojakFactory
    {
        private const int TransparencySize = 384;
        private const int TransparencyOffsetX = 140;
        private const int TransparencyOffsetY = 60;
        private const string ForegroundPath = "Resources/foreground.png";
        private const string BackgroundPath = "Resources/background.png";

        public const int ImageWidth = 800;
        public const int ImageHeight = 633;

        public static async Task<Image> CreateImageDefaultAsync(IEnumerable<string> layerPaths)
        {
            using var foreground = await Image.LoadAsync<Rgba32>(ForegroundPath);
            using var background = await Image.LoadAsync<Rgba32>(BackgroundPath);

            var layerImages = await Task.WhenAll(layerPaths.Select((layerPath) => Image.LoadAsync<Rgba32>(layerPath)));

            var result = new Image<Rgba32>(ImageWidth, ImageHeight, Rgba32.ParseHex("fff"));

            result.Mutate((ctx) =>
            {
                ctx.DrawImage(background, 1);
                foreach (var layerImage in layerImages)
                {
                    layerImage.Mutate((lCtx) =>
                    {
                        lCtx.Resize(TransparencySize, TransparencySize);
                    });
                    ctx.DrawImage(layerImage, new Point(TransparencyOffsetX, TransparencyOffsetY), 1);
                }
                ctx.DrawImage(foreground, 1);
            });


            foreach (var layerImage in layerImages) layerImage.Dispose();

            return result;
        }

        public static async Task<Image> CreateImageDynamicAsync(string layerPath)
        {
            using var foreground = await Image.LoadAsync<Rgba32>(ForegroundPath);
            using var background = await Image.LoadAsync<Rgba32>(BackgroundPath);
            using var layerImage = await Image.LoadAsync<Rgba32>(layerPath);

            var result = new Image<Rgba32>(ImageWidth, ImageHeight, Rgba32.ParseHex("fff"));

            result.Mutate((ctx) =>
            {
                ctx.DrawImage(background, 1);
                layerImage.Mutate((lCtx) =>
                {
                    lCtx.ResizeToTransparency(layerImage);
                });
                ctx.DrawImage(layerImage, new Point(TransparencyOffsetX, TransparencyOffsetY), 1);

                ctx.DrawImage(foreground, 1);
            });

            return result;
        }

        private static IImageProcessingContext ResizeToTransparency(this IImageProcessingContext ctx, IImage image)
        {
            double scale = (double)TransparencySize / (double)image.Width;
            double newHeight = image.Height * scale;

            return ctx.Resize(TransparencySize, (int)Math.Round(newHeight));
        }
    }
}
