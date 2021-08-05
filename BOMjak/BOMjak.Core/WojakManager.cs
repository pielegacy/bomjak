using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BOMjak.Core
{
    public class WojakManager
    {
        private const string PathResult = "img/bomjak.png";
        private const int CacheDurationMinutes = 30;

        public static async Task<Stream> GetCurrentAsync()
        {
            if (!File.Exists(PathResult) || File.GetCreationTimeUtc(PathResult) < DateTime.UtcNow.AddMinutes(-1 * CacheDurationMinutes))
            {
                var client = new BOMClient(new BOMClient.Settings
                {
                    RadarUrl = "ftp://ftp.bom.gov.au/anon/gen/radar",
                    RadarTransparenciesUrl = "ftp://ftp.bom.gov.au/anon/gen/radar_transparencies",
                    WorkingDirectory = "img"
                });

                var result = new List<string>();
                result.AddRange(await client.DownloadRadarTransparencies(Core.Model.LocationCode.IDR023));

                var radarOverlays = await client.GetRadarOverlaysByLocationCodeAsync(Core.Model.LocationCode.IDR023, 1);

                foreach (var file in radarOverlays)
                {
                    result.Add(await client.DownloadRadarOverlay(file));
                }

                await WojakProcessor.CreateAsync(result, PathResult);
            }

            return File.OpenRead(PathResult);
        }
    }
}
