namespace NLBE_Bot.Services;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using FMWOTB.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal class Bot(IDiscordClientWrapper discordClient, IServiceProvider serviceProvider,
		ICommandEventHandler commandHandler, IWeeklyEventService weeklyEventHandler,
		IGuildMemberEventHandler guildMemberService, IBotState botState,
		IChannelService channelService, IGuildProvider guildProvider, IUserService userService, IMessageService messageService,
		IMessageEventHandler messageHandler, IWeeklyEventService weeklyEventService, ILogger<Bot> logger, IPublicIpAddress publicIpAddress) : BackgroundService
{
	private readonly IDiscordClientWrapper _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly ICommandEventHandler _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
	private readonly IWeeklyEventService _weeklyEventHandler = weeklyEventHandler ?? throw new ArgumentNullException(nameof(weeklyEventHandler));
	private readonly IGuildMemberEventHandler _guildMemberHandler = guildMemberService ?? throw new ArgumentNullException(nameof(guildMemberService));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly IGuildProvider _guildProvider = guildProvider ?? throw new ArgumentNullException(nameof(guildProvider));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IMessageEventHandler _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
	private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	private readonly IWeeklyEventService _weeklyEventService = weeklyEventService ?? throw new ArgumentNullException(nameof(weeklyEventService));
	private readonly ILogger<Bot> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IPublicIpAddress _publicIpAddress = publicIpAddress ?? throw new ArgumentNullException(nameof(publicIpAddress));

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("NLBE Bot is starting.");

		string ipAddress = await _publicIpAddress.GetPublicIpAddressAsync();
		_logger.LogInformation("Ensure the public ip address {IpAddress} is allowed to access the WarGaming application.", ipAddress);

		try
		{
			// Register Bot commands.
			CommandsNextConfiguration commandsConfig = new()
			{
				StringPrefixes = [Constants.Prefix],
				EnableDms = false,
				EnableMentionPrefix = true,
				DmHelp = false,
				EnableDefaultHelp = false,
				Services = _serviceProvider
			};

			_discordClient.UseCommandsNext(commandsConfig).RegisterCommands<BotCommands>();

			RegisterEventHandlers();

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

	private void RegisterEventHandlers()
	{
		// Commands
		ICommandsNextExtension commands = _discordClient.GetCommandsNext();
		_commandHandler.Register(commands);

		// DiscordClient events
		_discordClient.Heartbeated += Discord_Heartbeated;
		_discordClient.Ready += Discord_Ready;

		// Guild member events
		_guildMemberHandler.Register(_discordClient);

		// Message events
		_messageHandler.Register(_discordClient);
	}

	private async Task Discord_Heartbeated(DiscordClient _, HeartbeatEventArgs e)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		_botState.HeartBeatCounter++;
		const int hourToCheck = 14;
		const DayOfWeek dayToCheck = DayOfWeek.Monday;

		if ((DateTime.Now.DayOfWeek != dayToCheck || DateTime.Now.Hour != hourToCheck) && _botState.HeartBeatCounter > 2)
		{
			//update usernames
			_botState.HeartBeatCounter = 0;
			bool update = false;
			if (_botState.LasTimeNamesWereUpdated.HasValue)
			{
				if (_botState.LasTimeNamesWereUpdated.Value.DayOfYear != DateTime.Now.DayOfYear)
				{
					update = true;
					_botState.LasTimeNamesWereUpdated = DateTime.Now;
				}
			}
			else
			{
				update = true;
				_botState.LasTimeNamesWereUpdated = DateTime.Now;
			}
			if (update)
			{
				try
				{
					await _userService.UpdateUsers();
				}
				catch (InternalServerErrorException ex)
				{
					string message = "\nERROR updating users:\nInternal server exception from api request\n" + ex.Message;
					await _messageService.SendThibeastmo(message, string.Empty, string.Empty);
					DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
					await bottestChannel.SendMessageAsync(message);
				}
				catch (Exception ex)
				{
					string message = "\nERROR updating users:\n" + ex.Message;
					await _messageService.SendThibeastmo(message, string.Empty, string.Empty);
					DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
					await bottestChannel.SendMessageAsync(message);
				}
			}
		}
		else if (DateTime.Now.DayOfWeek == dayToCheck && DateTime.Now.Hour == hourToCheck && _botState.HeartBeatCounter == 2)//14u omdat wotb ook wekelijks op maandag 14u restart
		{
			//We have a weekly winner
			string winnerMessage = "Het wekelijkse event is afgelopen.";
			DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
			try
			{
				_logger.LogInformation(winnerMessage);

				await _weeklyEventHandler.ReadWeeklyEvent();
				if (_weeklyEventHandler.WeeklyEvent.StartDate.DayOfYear == DateTime.Now.DayOfYear - 7)//-7 omdat het dan zeker een nieuwe week is maar niet van twee weken geleden
				{
					winnerMessage += "\nNa 1 week...";
					WeeklyEventItem weeklyEventItemMostDMG = _weeklyEventHandler.WeeklyEvent.WeeklyEventItems.Find(weeklyEventItem => weeklyEventItem.WeeklyEventType == WeeklyEventType.Most_damage);
					if (weeklyEventItemMostDMG.Player != null && weeklyEventItemMostDMG.Player.Length > 0)
					{
						foreach (KeyValuePair<ulong, DiscordGuild> guild in _guildProvider.Guilds)
						{
							if (guild.Key is Constants.NLBE_SERVER_ID or Constants.DA_BOIS_ID)
							{
								await _weeklyEventService.WeHaveAWinner(guild.Value, weeklyEventItemMostDMG, _weeklyEventHandler.WeeklyEvent.Tank);
								break;
							}
						}
					}
				}
				await bottestChannel.SendMessageAsync(winnerMessage);
				await _messageService.SendThibeastmo(winnerMessage, string.Empty, string.Empty);
			}
			catch (Exception ex)
			{
				string message = winnerMessage + "\nERROR:\n" + ex.Message;
				await bottestChannel.SendMessageAsync(message);
				await _messageService.SendThibeastmo(message, string.Empty, string.Empty);
			}
		}
	}

	private Task Discord_Ready(DiscordClient sender, ReadyEventArgs e)
	{
		foreach (KeyValuePair<ulong, DiscordGuild> guild in _guildProvider.Guilds)
		{
			if (!guild.Key.Equals(Constants.NLBE_SERVER_ID) && !guild.Key.Equals(Constants.DA_BOIS_ID))
			{
				guild.Value.LeaveAsync();
			}
		}
		_logger.Log(LogLevel.Information, "Client (v{Version}) is ready to process events.", Constants.version);

		return Task.CompletedTask;
	}
}
