namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using NLBE_Bot;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

internal class BotEventHandlers(ICommandEventHandler commandHandler, IGuildMemberEventHandler guildMemberHandler, IMessageEventHandler messageHandler,
								IUserService userService, IWeeklyEventService weeklyEventService, ILogger<BotEventHandlers> logger,
								IErrorHandler errorHandler) : IBotEventHandlers
{
	private readonly ICommandEventHandler _commandHandler = commandHandler;
	private readonly IGuildMemberEventHandler _guildMemberHandler = guildMemberHandler;
	private readonly IMessageEventHandler _messageHandler = messageHandler;
	private readonly IUserService _userService = userService;
	private readonly IWeeklyEventService _weeklyEventService = weeklyEventService;
	private readonly ILogger<BotEventHandlers> _logger = logger;
	private readonly IErrorHandler _errorHandler = errorHandler;

	private IBotState _botState;
	private int _heartBeatCounter;

	public void Register(IDiscordClient client, IBotState botState)
	{
		_botState = botState;

		// Commands
		ICommandsNextExtension commands = client.GetCommandsNext();
		_commandHandler.Register(commands);

		// Guild member events
		_guildMemberHandler.Register(client);

		// Message events
		_messageHandler.Register(client);

		// Generic events
		client.Heartbeated += OnHeartbeated;
		client.Ready += OnReady;
	}

	internal Task OnReady(DiscordClient discordClient, ReadyEventArgs __)
	{
		foreach (KeyValuePair<ulong, DiscordGuild> guild in discordClient.Guilds)
		{
			if (!guild.Key.Equals(Constants.NLBE_SERVER_ID) && !guild.Key.Equals(Constants.DA_BOIS_ID)) // TODO: move to config.
			{
				guild.Value.LeaveAsync();
			}
		}

		_logger.LogInformation("Client (v{Version}) is ready to process events.", Constants.version);

		return Task.CompletedTask;
	}

	internal async Task OnHeartbeated(DiscordClient _, HeartbeatEventArgs e)
	{
		_heartBeatCounter++;

		if (_heartBeatCounter == 1) // Skip the first heartbeat, as it is triggered on startup.
		{
			return;
		}

		if (_botState.IgnoreEvents)
		{
			return;
		}

		DateTime now = DateTime.Now;

		if (ShouldUpdateUsernames(now, _botState.LasTimeNamesWereUpdated))
		{
			await UpdateUsernames();
		}

		if (ShouldAnnounceWeeklyWinner(now, _botState.LastWeeklyWinnerAnnouncement))
		{
			await _weeklyEventService.AnnounceWeeklyWinner();
		}
	}

	private static bool ShouldUpdateUsernames(DateTime now, DateTime? lastUpdate)
	{
		// Run once per day, at or after 00:00, but only if not already run today.
		return !lastUpdate.HasValue || lastUpdate.Value.Date != now.Date;
	}

	private static bool ShouldAnnounceWeeklyWinner(DateTime now, DateTime? lastAnnouncement)
	{
		// Run once per week, on Monday at or after 14:00, but only if not already run this week.
		bool isMondayAtOrAfter14 = now.DayOfWeek == DayOfWeek.Monday && now.Hour >= 14;
		bool notAlreadyAnnouncedThisWeek = !lastAnnouncement.HasValue ||
			CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(lastAnnouncement.Value, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
			!= CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

		return isMondayAtOrAfter14 && notAlreadyAnnouncedThisWeek;
	}

	private async Task UpdateUsernames()
	{
		bool update = false;
		DateTime now = DateTime.Now;

		if (_botState.LasTimeNamesWereUpdated.HasValue)
		{
			if (_botState.LasTimeNamesWereUpdated.Value.DayOfYear != now.DayOfYear)
			{
				update = true;
				_botState.LasTimeNamesWereUpdated = now;
			}
		}
		else
		{
			update = true;
			_botState.LasTimeNamesWereUpdated = now;
		}

		if (update)
		{
			try
			{
				await _userService.UpdateUsers();
			}
			catch (Exception ex)
			{
				string message = "\nERROR updating users:\n" + ex.Message;
				await _errorHandler.HandleErrorAsync(message, ex);
			}
		}
	}
}
