namespace NLBE_Bot.Services;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

internal class Bot(IDiscordClient discordClient, IBotEventHandlers eventHandlers, ILogger<Bot> logger, IPublicIpAddress publicIpAddress, IServiceProvider provider) : BackgroundService
{
	private readonly IDiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly IBotEventHandlers _eventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
	private readonly ILogger<Bot> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IPublicIpAddress _publicIpAddress = publicIpAddress ?? throw new ArgumentNullException(nameof(publicIpAddress));
	private readonly IServiceProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("NLBE Bot is starting.");

		string ipAddress = await _publicIpAddress.GetPublicIpAddressAsync();
		_logger.LogInformation("Ensure the public ip address {IpAddress} is allowed to access the WarGaming application.", ipAddress);

		try
		{
			CommandsNextConfiguration commandsConfig = new()
			{
				StringPrefixes = [Constants.Prefix],
				EnableDms = false,
				EnableMentionPrefix = true,
				DmHelp = false,
				EnableDefaultHelp = false,
				Services = _provider
			};

			_discordClient.UseCommandsNext(commandsConfig).RegisterCommands<BotCommands>();
			_eventHandlers.Register(_discordClient);

			DiscordActivity activity = new(Constants.Prefix, ActivityType.ListeningTo);
			await _discordClient.ConnectAsync(activity, UserStatus.Online);

			await Task.Delay(-1, stoppingToken);
		}
		catch (OperationCanceledException ex)
		{
			_logger.LogInformation(ex, "NLBE Bot was cancelled gracefully.");
		}
		finally
		{
			_logger.LogInformation("NLBE Bot is stopped.");
		}
	}
}
