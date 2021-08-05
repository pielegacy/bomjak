using BOMjak.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BOMjak.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Directory.Delete("img", true);

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

            await WojakProcessor.CreateAsync(result, "img/test.png");
        }
    }
}
