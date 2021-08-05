using BOMjak.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BOMjak.Core
{
    public class BOMClient
    {
        private string RadarUrl { get; }
        private string RadarTransparenciesUrl { get; }
        private string WorkingDirectory { get; }

        public class Settings
        {
            public string WorkingDirectory { get; set; }
            public string RadarUrl { get; set; }
            public string RadarTransparenciesUrl { get; set; }
        }

        private Regex GetRadarFileRegex(LocationCode locationCode) => new Regex($"(.*){locationCode}.T.(.*).png");

        private string GetWorkingDirectoryFilePath(string fileName) => Path.Combine(Path.GetFullPath(WorkingDirectory), fileName);

        public BOMClient(Settings settings)
        {
            RadarUrl = settings.RadarUrl;
            RadarTransparenciesUrl = settings.RadarTransparenciesUrl;
            WorkingDirectory = settings.WorkingDirectory;

            Directory.CreateDirectory(WorkingDirectory);
        }

        public async Task<IEnumerable<string>> GetRadarOverlaysByLocationCodeAsync(LocationCode locationCode, int limit = 5)
        {
            var request = WebRequest.Create(RadarUrl) as FtpWebRequest;
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            var response = await request.GetResponseAsync();

            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                var regex = GetRadarFileRegex(locationCode);
                var files = (await streamReader.ReadToEndAsync()).Split(Environment.NewLine);
                return files
                    .Where(file => regex.IsMatch(file))
                    .Select(file => file.Split('/').Last())
                    .OrderBy(file => file)
                    .TakeLast(limit)
                    .ToList();
            }
        }

        public async Task<IEnumerable<string>> DownloadRadarTransparencies(LocationCode locationCode)
        {
            var result = new List<string>();
            
            foreach (var transparency in Enum.GetValues(typeof(Transparency)))
            {
                var fileName = $"{locationCode}.{transparency.ToString().ToLower()}.png";
                var fileUrl = $"{RadarTransparenciesUrl}/{fileName}";
                result.Add(await DownloadFile(fileName, fileUrl));
            }

            return result;
        }

        public async Task<string> DownloadRadarOverlay(string fileName)
        {
            var fileUrl = $"{RadarUrl}/{fileName}";
            return await DownloadFile(fileName, fileUrl);
        }

        private async Task<string> DownloadFile(string fileName, string fileUrl)
        {
            var request = WebRequest.Create(fileUrl) as FtpWebRequest;
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            var response = await request.GetResponseAsync();
            var outputPath = GetWorkingDirectoryFilePath(fileName);
            using (var fileStream = File.OpenWrite(outputPath))
            {
                await response.GetResponseStream().CopyToAsync(fileStream);
            }
            return outputPath;
        }
    }
}
