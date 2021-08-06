using BOMjak.Core.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOMjak.Core
{
    public class BOMJakManager
    {
        private const int CacheDurationMinutes = 15;

        public LocationCode LocationCode { get; }
        public string ImagePathStatic { get; }
        public string ImagePathAnimated { get; }

        public BOMJakManager(LocationCode locationCode)
        {
            const string PathBase = "img";
            LocationCode = locationCode;
            ImagePathStatic = $"{PathBase}/{locationCode}.png";
            ImagePathAnimated = $"{PathBase}/{locationCode}.gif";
        }

        public async Task<Stream> CreateStaticAsync()
        {
            if (!File.Exists(ImagePathStatic) || File.GetCreationTimeUtc(ImagePathStatic) < DateTime.UtcNow.AddMinutes(-1 * CacheDurationMinutes))
            {
                var client = new BOMClient(new BOMClient.Settings
                {
                    RadarUrl = "ftp://ftp.bom.gov.au/anon/gen/radar",
                    RadarTransparenciesUrl = "ftp://ftp.bom.gov.au/anon/gen/radar_transparencies",
                    WorkingDirectory = "img"
                });

                var result = new List<string>();
                result.AddRange(await client.DownloadRadarTransparencies(LocationCode));

                var radarOverlays = await client.GetRadarOverlaysByLocationCodeAsync(LocationCode, 1);

                foreach (var file in radarOverlays)
                {
                    result.Add(await client.DownloadRadarOverlay(file));
                }

                using var image = await WojakProcessor.CreateImageAsync(result);
                await image.SaveAsync(ImagePathStatic);
            }

            return File.OpenRead(ImagePathStatic);
        }

        public async Task<Stream> CreateAnimatedAsync()
        {
            if (!File.Exists(ImagePathAnimated) || File.GetCreationTimeUtc(ImagePathAnimated) < DateTime.UtcNow.AddMinutes(-1 * CacheDurationMinutes))
            {
                var client = new BOMClient(new BOMClient.Settings
                {
                    RadarUrl = "ftp://ftp.bom.gov.au/anon/gen/radar",
                    RadarTransparenciesUrl = "ftp://ftp.bom.gov.au/anon/gen/radar_transparencies",
                    WorkingDirectory = "img"
                });

                var transparencies = new List<string>();
                transparencies.AddRange(await client.DownloadRadarTransparencies(LocationCode));

                var radarOverlays = await client.GetRadarOverlaysByLocationCodeAsync(LocationCode, 6);

                var image = new Image<Rgba32>(WojakProcessor.ImageWidth, WojakProcessor.ImageHeight);

                foreach (var file in radarOverlays)
                {
                    var frameLayers = transparencies.Concat(new[] { await client.DownloadRadarOverlay(file) });
                    using var frameImage = await WojakProcessor.CreateImageAsync(frameLayers);
                    var newFrame = image.Frames.AddFrame(frameImage.Frames[0]);
                    newFrame.Metadata.GetGifMetadata().FrameDelay = 100;
                }

                image.Frames.RemoveFrame(0);
                image.Metadata.GetGifMetadata().RepeatCount = 0;

                await image.SaveAsGifAsync(ImagePathAnimated);
            }

            return File.OpenRead(ImagePathAnimated);
        }
    }
}
