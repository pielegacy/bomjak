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
    public class WojakProcessor
    {
        private const int TransparencySize = 384;
        private const int TransparencyOffsetX = 140;
        private const int TransparencyOffsetY = 20;
        private const string ForegroundPath = "Resources/foreground.png";

        public const int ImageWidth = 800;
        public const int ImageHeight = 633;

        public static async Task<Image> CreateImageAsync(IEnumerable<string> layerPaths)
        {
            using var foreground = await Image.LoadAsync<Rgba32>(ForegroundPath);

            var layerImages = await Task.WhenAll(layerPaths.Select((layerPath) => Image.LoadAsync<Rgba32>(layerPath)));

            var result = new Image<Rgba32>(ImageWidth, ImageHeight, Rgba32.ParseHex("fff"));

            result.Mutate((ctx) =>
            {
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
    }
}
