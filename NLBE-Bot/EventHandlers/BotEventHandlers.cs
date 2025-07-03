namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLBE_Bot;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal class BotEventHandlers(ICommandEventHandler commandHandler,
								IGuildMemberEventHandler guildMemberHandler,
								IMessageEventHandler messageHandler,
								ITimedEventHandler timedEventHandler,
								ILogger<BotEventHandlers> logger,
								IErrorHandler errorHandler,
								IConfiguration configuration) : IBotEventHandlers
{
	private readonly ICommandEventHandler _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
	private readonly IGuildMemberEventHandler _guildMemberHandler = guildMemberHandler ?? throw new ArgumentNullException(nameof(guildMemberHandler));
	private readonly IMessageEventHandler _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
	private readonly ITimedEventHandler _timedEventHandler = timedEventHandler ?? throw new ArgumentNullException(nameof(timedEventHandler));
	private readonly ILogger<BotEventHandlers> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

	private int _heartBeatCounter;

	public void Register(IDiscordClient client, IBotState botState)
	{
		_ = client ?? throw new ArgumentNullException(nameof(client));
		_ = botState ?? throw new ArgumentNullException(nameof(botState));

		// Commands
		ICommandsNextExtension commands = client.GetCommandsNext();
		_commandHandler.Register(commands);

		// Guild member events
		_guildMemberHandler.Register(client, botState);

		// Message events
		_messageHandler.Register(client);

		// Generic events
		client.Heartbeated += OnHeartbeated;
		client.Ready += OnReady;
		client.ClientErrored += OnClientErrored;
		client.SocketClosed += OnSocketClosed;
	}

	private async Task OnClientErrored(DiscordClient sender, ClientErrorEventArgs e)
	{
		await _errorHandler.HandleErrorAsync($"Error with event ({e.EventName}):\n", e.Exception);
	}

	private Task OnHeartbeated(DiscordClient _, HeartbeatEventArgs e)
	{
		_logger.LogDebug("Received heartbeat. Ping: {Ping}. Timestamp: {Timestamp}.", e.Ping, e.Timestamp);
		return HandleHeartbeated(DateTime.Now);
	}

	private Task OnReady(DiscordClient discordClient, ReadyEventArgs _)
	{
		return HandleReady(new DiscordClientWrapper(discordClient));
	}

	private Task OnSocketClosed(DiscordClient sender, SocketCloseEventArgs e)
	{
		return HandleSocketClosed(e.CloseCode, e.CloseMessage);
	}

	internal async Task HandleHeartbeated(DateTime now)
	{
		_heartBeatCounter++;

		if (_heartBeatCounter == 1) // Skip the first heartbeat, as it is triggered on startup.
		{
			return;
		}

		await _timedEventHandler.Execute(now);
	}

	internal Task HandleReady(IDiscordClient discordClient)
	{
		foreach (KeyValuePair<ulong, IDiscordGuild> guild in discordClient.Guilds)
		{
			ulong serverId = _configuration.GetValue<ulong>("NLBEBot:ServerId");

			if (!guild.Key.Equals(serverId))
			{
				guild.Value.LeaveAsync();
			}
		}

		_logger.LogInformation("Client (v{Version}) is ready to process events.", Constants.Version);

		return Task.CompletedTask;
	}

	internal Task HandleSocketClosed(int closeCode, string closeMessage)
	{
		if (closeCode != 1000) // Abnormal closure
		{
			return _errorHandler.HandleErrorAsync($"Socket closed unexpectedly. Code: {closeCode}. Reason: {closeMessage}.");
		}

		_logger.LogInformation("Socket closed normally. Code: {CloseCode}. Reason: {CloseMessage}.", closeCode, closeMessage);
		return Task.CompletedTask;
	}
}
