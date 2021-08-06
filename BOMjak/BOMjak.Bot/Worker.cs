using BOMjak.Core;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BOMjak.Bot
{
    public class Worker : BackgroundService
    {
        private const string TokenEnvironmentVariable = "BOMJAK_BOT_TOKEN";
        private readonly ILogger<Worker> _logger;

        public DiscordSocketClient DiscordClient { get; }
        private string Token { get; }

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            DiscordClient = new DiscordSocketClient();

            Token = configuration[TokenEnvironmentVariable];

            DiscordClient.MessageReceived += MessageReceived;
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            _logger.LogInformation(arg.Content);
            if (DiscordClient.GetChannel(arg.Channel.Id) is SocketTextChannel sourceChannel
                && arg.Content.ToLower().Contains("bomjak"))
            {
                try
                {
                    var locationCode = Core.Model.LocationCode.IDR023;
                    _logger.LogInformation($"Getting BOMjak for {locationCode}");
                    var manager = new BOMJakManager(locationCode);
                    var wojakTask = manager.CreateStaticAsync();
                    await sourceChannel.SendMessageAsync("Let me get that for you");
                    var wojak = await wojakTask;
                    _logger.LogInformation("BOMjak generated, sending now.");
                    await sourceChannel.SendFileAsync(wojak, $"{locationCode}.{DateTime.Now.Ticks}.png", string.Empty);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Wojak generation failed");
                    await sourceChannel.SendMessageAsync("Something went wrong, please try again later");
                }
            }
        }

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
