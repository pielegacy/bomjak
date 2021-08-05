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

        public static async Task CreateAsync(IEnumerable<string> layerPaths, string outputPath)
        {
            using var foreground = await Image.LoadAsync<Rgba32>(ForegroundPath);

            var layerImages = await Task.WhenAll(layerPaths.Select((layerPath) => Image.LoadAsync<Rgba32>(layerPath)));

            using var result = new Image<Rgba32>(800, 633, Rgba32.ParseHex("fff"));

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


            await result.SaveAsync(outputPath);

            foreach (var layerImage in layerImages) layerImage.Dispose();
        }
    }
}
