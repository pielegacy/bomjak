using BOMjak.Core;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BOMjak.Bot
{
    public class Worker : BackgroundService
    {
        private const string LEGACY_PREFIX = "bomjak";
        private const string NEW_PREFIX = "bureaujak";
        private readonly string[] _prefixes = new[]
        {
            LEGACY_PREFIX,
            NEW_PREFIX,
        };
        private const string TokenEnvironmentVariable = "BOMJAK_BOT_TOKEN";
        private const int AttachmentScanMaximum = 5;
        private readonly ILogger<Worker> _logger;

        private readonly string[] _possibleResponses = new string[]
        {
            "ok...",
            "👍"
        };

        private string Response => _possibleResponses[Random.Next(_possibleResponses.Length)];

        private DiscordSocketClient DiscordClient { get; }
        private HttpClient HttpClient { get; }
        private Random Random { get; }
        private string Token { get; }
        public string HelpText { get; }
        private List<ProcessorDelegate> Processors { get; }

        private delegate Task<bool> ProcessorDelegate(string text, SocketMessage arg, ISocketMessageChannel channel);

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            DiscordClient = new DiscordSocketClient();
            HttpClient = new HttpClient();
            Random = new Random();

            Token = configuration[TokenEnvironmentVariable];
            HelpText = File.ReadAllText(configuration["Resources:HelpFile"]);

            DiscordClient.MessageReceived += MessageReceived;
            Processors = new List<ProcessorDelegate>
            {
                TryProcessHelpAsync,
                TryProcessCustomAsync,
                TryProcessLastAsync,
                TryProcessStandardAsync
            };
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            _logger.LogInformation(arg.Content);
            if (DiscordClient.GetChannel(arg.Channel.Id) is ISocketMessageChannel sourceChannel)
            {
                try
                {
                    var messageText = arg.Content.ToLower().Trim();
                    if (!_prefixes.Any(prefix => messageText.StartsWith(prefix))) return;

                    if (messageText.StartsWith(LEGACY_PREFIX))
                    {
                        _ = sourceChannel.SendMessageAsync($"I'LL HELP YOU OUT DOG BUT THE NAME'S `{NEW_PREFIX}` NOW...");
                    }

                    foreach (var processor in Processors)
                    {
                        if (await processor(messageText, arg, sourceChannel)) return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Wojak generation failed");
                    await sourceChannel.SendMessageAsync("Something went wrong, please try again later");
                }
            }
        }

        private async Task<bool> TryProcessCustomAsync(string text, SocketMessage arg, ISocketMessageChannel channel)
        {
            var imageAttachment = GetImageAttachment(arg);
            if (imageAttachment is null) return false;

            var tempFile = Path.GetTempFileName();
            try
            {
                var sourceUrl = imageAttachment.Url;
                _logger.LogInformation($"Getting custom BOMjak for {sourceUrl}");
                var manager = new BOMJakManager(0);
                var response = await HttpClient.GetAsync(sourceUrl);
                using (var fileStream = File.OpenWrite(tempFile))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
                var wojakTask = manager.CreateCustomAsync(tempFile);
                await channel.SendMessageAsync(Response);
                var wojak = await wojakTask;
                _logger.LogInformation("BOMjak generated, sending now.");
                await channel.SendFileAsync(wojak, $"custom.{DateTime.Now.Ticks}.png", string.Empty);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate customer wojak");
                return false;
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        private async Task<bool> TryProcessLastAsync(string messageText, SocketMessage arg, ISocketMessageChannel channel)
        {
            if (!messageText.Contains("last")) return false;

            var tempFile = Path.GetTempFileName();
            try
            {
                var messages = await channel.GetMessagesAsync(5).FlattenAsync();

                var lastAttachment = messages
                    .OrderByDescending(message => message.CreatedAt)
                    .Select(message => GetImageAttachment(message))
                    .FirstOrDefault(attachment => attachment != null);

                if (lastAttachment is null)
                {
                    await channel.SendMessageAsync($"<@{arg.Author.Id}> i cant see any messages with images??? ok...");
                    return true;
                }

                var sourceUrl = lastAttachment.Url;
                _logger.LogInformation($"Getting custom BOMjak for {sourceUrl}");
                var manager = new BOMJakManager(0);
                var response = await HttpClient.GetAsync(sourceUrl);
                using (var fileStream = File.OpenWrite(tempFile))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
                var wojakTask = manager.CreateCustomAsync(tempFile);
                await channel.SendMessageAsync(Response);
                var wojak = await wojakTask;
                _logger.LogInformation("BOMjak generated, sending now.");
                await channel.SendFileAsync(wojak, $"custom.{DateTime.Now.Ticks}.png", string.Empty);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate customer wojak");
                return false;
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        private async Task<bool> TryProcessStandardAsync(string text, SocketMessage arg, ISocketMessageChannel channel)
        {
            var locationCode = Core.Model.LocationCode.IDR023;
            _logger.LogInformation($"Getting BOMjak for {locationCode}");
            var manager = new BOMJakManager(locationCode);
            var wojakTask = manager.CreateAnimatedAsync();
            await channel.SendMessageAsync(Response);
            var wojak = await wojakTask;
            _logger.LogInformation("BOMjak generated, sending now.");
            await channel.SendFileAsync(wojak, $"{locationCode}.{DateTime.Now.Ticks}.gif", string.Empty);
            return true;
        }

        private async Task<bool> TryProcessHelpAsync(string text, SocketMessage arg, ISocketMessageChannel channel)
        {
            if (!text.Contains("help")) return false;
            await channel.SendMessageAsync(HelpText);
            return true;
        }

        private static IAttachment GetImageAttachment(IMessage arg) => arg?.Attachments.FirstOrDefault(a => a.Width.HasValue);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (stoppingToken.IsCancellationRequested) return;

                await DiscordClient.LoginAsync(Discord.TokenType.Bot, Token);
                await DiscordClient.StartAsync();

                _logger.LogInformation("BOMjak connected");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BOMjak failed to connect to Discord");
            }
            finally
            {
                _logger.LogInformation("BOMjak shutting down");
            }
        }

        public override void Dispose()
        {
            DiscordClient?.Dispose();
            base.Dispose();
        }
    }
}
