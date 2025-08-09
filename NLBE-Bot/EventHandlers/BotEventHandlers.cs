namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

internal class BotEventHandlers(ICommandEventHandler commandHandler,
								IGuildMemberEventHandler guildMemberHandler,
								IMessageEventHandler messageHandler,
								IJob<VerifyServerNicknamesJob> verifyServerNicknamesJob,
								IJob<AnnounceWeeklyWinnerJob> announceWeeklyWinnerJob,
								ILogger<BotEventHandlers> logger,
								IOptions<BotOptions> options) : IBotEventHandlers
{
	private readonly ICommandEventHandler _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
	private readonly IGuildMemberEventHandler _guildMemberHandler = guildMemberHandler ?? throw new ArgumentNullException(nameof(guildMemberHandler));
	private readonly IMessageEventHandler _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
	private readonly IJob<VerifyServerNicknamesJob> _verifyServerNicknamesJob = verifyServerNicknamesJob ?? throw new ArgumentNullException(nameof(verifyServerNicknamesJob));
	private readonly IJob<AnnounceWeeklyWinnerJob> _announceWeeklyWinnerJob = announceWeeklyWinnerJob ?? throw new ArgumentNullException(nameof(announceWeeklyWinnerJob));
	private readonly ILogger<BotEventHandlers> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

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

	private Task OnClientErrored(DiscordClient sender, ClientErrorEventArgs e)
	{
		HandleClienErrored(e.EventName, e.Exception);
		return Task.CompletedTask;
	}

	private Task OnHeartbeated(DiscordClient _, HeartbeatEventArgs e)
	{
		return HandleHeartbeated(e.Ping, e.Timestamp, DateTime.Now);
	}

	private Task OnReady(DiscordClient discordClient, ReadyEventArgs _)
	{
		return HandleReady(new DiscordClientWrapper(discordClient));
	}

	private Task OnSocketClosed(DiscordClient sender, SocketCloseEventArgs e)
	{
		return HandleSocketClosed(e.CloseCode, e.CloseMessage);
	}

	internal void HandleClienErrored(string eventName, Exception exception)
	{
		_logger.LogError(exception, "An error occurred in the Discord client event: {EventName}.", eventName);
	}

	internal async Task HandleHeartbeated(int ping, DateTimeOffset timestamp, DateTime now)
	{
		_logger.LogDebug("Received heartbeat. Ping: {Ping}. Timestamp: {Timestamp}.", ping, timestamp);

		_heartBeatCounter++;

		if (_heartBeatCounter == 1) // Skip the first heartbeat, as it is triggered on startup.
		{
			return;
		}

		await _verifyServerNicknamesJob.Execute(now);
		await _announceWeeklyWinnerJob.Execute(now);
	}

	internal async Task HandleReady(IDiscordClient discordClient)
	{
		try
		{
			foreach (KeyValuePair<ulong, IDiscordGuild> guild in from KeyValuePair<ulong, IDiscordGuild> guild in discordClient.Guilds
																 where !guild.Key.Equals(_options.ServerId)
																 select guild)
			{
				_logger.LogWarning("Bot is not configured to handle guild {GuildId}. Leaving the guild.", guild.Key);
				await guild.Value.LeaveAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while handling the Ready event. Could not leave non-whitelisted guilds.");
		}

		_logger.LogInformation("Client (v{Version}) is ready to process events.", Constants.Version);
	}

	internal Task HandleSocketClosed(int closeCode, string closeMessage)
	{
		if (closeCode != 1000) // Abnormal closure
		{
			_logger.LogError("Socket closed unexpectedly. Code: {CloseCode}. Reason: {CloseMessage}.", closeCode, closeMessage);
		}
		else
		{
			_logger.LogInformation("Socket closed normally. Code: {CloseCode}. Reason: {CloseMessage}.", closeCode, closeMessage);
		}

		return Task.CompletedTask;
	}
}
