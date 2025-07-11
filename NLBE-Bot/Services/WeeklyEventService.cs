namespace NLBE_Bot.Services;

using DiscordHelper;
using DSharpPlus.Entities;
using FMWOTB.Tools.Replays;
using Microsoft.Extensions.Logging;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class WeeklyEventService(IChannelService channelService, IUserService userService, IGuildProvider guildProvider, IBotState botState, ILogger<BotEventHandlers> logger, IErrorHandler errorHandler) : IWeeklyEventService
{
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IGuildProvider _guildProvider = guildProvider;
	private readonly ILogger<BotEventHandlers> _logger = logger;

	public DiscordMessage DiscordMessage
	{
		get; set;
	} //The last message in Weekly events

	public WeeklyEvent WeeklyEvent
	{
		get; set;
	}

	public async Task AnnounceWeeklyWinner()
	{
		DateTime now = DateTime.Now;
		StringBuilder winnerMessage = new("Het wekelijkse event is afgelopen.");

		try
		{
			_logger.LogInformation(winnerMessage.ToString());

			await ReadWeeklyEvent();

			if (WeeklyEvent.StartDate.DayOfYear == now.DayOfYear - 7)
			{
				winnerMessage.AppendLine("Na 1 week...");
				WeeklyEventItem weeklyEventItemMostDMG = WeeklyEvent.WeeklyEventItems.Find(weeklyEventItem => weeklyEventItem.WeeklyEventType == WeeklyEventType.Most_damage);

				if (weeklyEventItemMostDMG.Player != null && weeklyEventItemMostDMG.Player.Length > 0)
				{
					foreach (KeyValuePair<ulong, DiscordGuild> guild in from KeyValuePair<ulong, DiscordGuild> guild in _guildProvider.Guilds
																		where guild.Key is Constants.NLBE_SERVER_ID or Constants.DA_BOIS_ID
																		select guild)
					{
						await WeHaveAWinner(guild.Value, weeklyEventItemMostDMG, WeeklyEvent.Tank);
					}
				}
			}

			DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
			await bottestChannel.SendMessageAsync(winnerMessage.ToString());
		}
		catch (Exception ex)
		{
			string message = winnerMessage + "\nERROR:\n" + ex.Message;
			await _errorHandler.HandleErrorAsync(message, ex);
		}
	}
	public async Task WeHaveAWinner(DiscordGuild guild, WeeklyEventItem weeklyEventItemMostDMG, string tank)
	{
		bool userNotFound = true;
		IReadOnlyCollection<DiscordMember> members = await guild.GetAllMembersAsync();
		if (weeklyEventItemMostDMG.Player != null)
		{
			string weeklyEventItemMostDMGPlayer = weeklyEventItemMostDMG.Player
				.Replace("\\", string.Empty)
				.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_')
				.ToLower();
			foreach (DiscordMember member in members)
			{
				if (!member.IsBot)
				{
					Tuple<string, string> gebruiker = _userService.GetIGNFromMember(member.DisplayName);
					string x = gebruiker.Item2
						.Replace("\\", string.Empty)
						.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_')
						.ToLower();
					if (x == weeklyEventItemMostDMGPlayer
						|| (member.Id == Constants.THIBEASTMO_ID
							&& guild.Id == Constants.DA_BOIS_ID))
					{
						userNotFound = false;

						_botState.WeeklyEventWinner = new Tuple<ulong, DateTime>(member.Id, DateTime.Now);

						try
						{
							await member.SendMessageAsync("Hallo " + member.Mention + ",\n\nProficiat! Je hebt het wekelijkse event gewonnen van de **" + tank + "** met **" + weeklyEventItemMostDMG.Value + "** damage.\n" +
														  "Dit wilt zeggen dat jij de tank voor het wekelijkse event mag kiezen.\n" +
														  "Je kan je keuze maken door enkel de naam van de tank naar mij te sturen. Indien ik de tank niet kan vinden dan zal ik je voorthelpen.\n" +
														  "De enige voorwaarde is wel dat je niet een recent gekozen tank opnieuw kiest."
														  + "\n\nSucces met je keuze!");
						}
						catch (Exception ex)
						{
							await _errorHandler.HandleErrorAsync("Could not send private message towards winner of weekly event.", ex);
						}
						try
						{
							DiscordChannel algemeenChannel = await _channelService.GetAlgemeenChannel();
							if (algemeenChannel != null)
							{
								await algemeenChannel.SendMessageAsync("Feliciteer **" + weeklyEventItemMostDMG.Player.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_').adaptToDiscordChat() + "** want hij heeft het wekelijkse event gewonnen! **Proficiat!**" +
																	   "\n" +
																	   "`" + tank + "` met `" + weeklyEventItemMostDMG.Value + "` damage" +
																	   "\n\n" +
																	   "We wachten nu af tot de winnaar een nieuwe tank kiest.");
							}
						}
						catch (Exception ex)
						{
							await _errorHandler.HandleErrorAsync("Could not send message in algemeen channel for weekly event winner announcement.", ex);
						}
						break;
					}
				}
			}
		}
		else
		{
			DiscordChannel algemeenChannel = await _channelService.GetAlgemeenChannel();
			if (algemeenChannel != null)
			{
				await algemeenChannel.SendMessageAsync("Het wekelijkse event is gedaan, helaas heeft er __niemand__ deelgenomen en is er dus geen winnaar.");
			}
		}
		DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
		if (userNotFound)
		{
			string message = "Weekly event winnaar was niet gevonden! Je zal het zelf moeten regelen met het `weekly` commando.";
			if (bottestChannel != null)
			{
				await bottestChannel.SendMessageAsync(message);
			}
			else
			{
				await _errorHandler.HandleErrorAsync(message);
			}
		}
		else
		{
			string message = "Weekly event winnaar gevonden!";
			if (bottestChannel != null)
			{
				await bottestChannel.SendMessageAsync(message);
			}
			else
			{
				await _errorHandler.HandleErrorAsync(message);
			}
		}
	}

	public async Task<List<WeeklyEventType>> CheckAndHandleWeeklyEvent(WGBattle battle)
	{
		List<WeeklyEventType> weeklyEventTypes = [];
		if (battle.vehicle == WeeklyEvent.Tank)
		{
			if (WeeklyEvent.WeeklyEventItems[0].Value < battle.details.damage_made)
			{
				weeklyEventTypes.Add(WeeklyEventType.Most_damage);
				WeeklyEvent.WeeklyEventItems[0] = new WeeklyEventItem(battle.details.damage_made, battle.player_name, battle.view_url, weeklyEventTypes[weeklyEventTypes.Count - 1]);
			}
			if (WeeklyEvent.WeeklyEventItems[1].Value < battle.exp_base)
			{
				weeklyEventTypes.Add(WeeklyEventType.Most_exp);
				WeeklyEvent.WeeklyEventItems[1] = new WeeklyEventItem(battle.exp_base, battle.player_name, battle.view_url, weeklyEventTypes[weeklyEventTypes.Count - 1]);
			}
			if (WeeklyEvent.WeeklyEventItems[2].Value < battle.credits_base)
			{
				weeklyEventTypes.Add(WeeklyEventType.Most_credits);
				WeeklyEvent.WeeklyEventItems[2] = new WeeklyEventItem(battle.credits_base, battle.player_name, battle.view_url, weeklyEventTypes[weeklyEventTypes.Count - 1]);
			}
			if (WeeklyEvent.WeeklyEventItems[3].Value < battle.details.damage_blocked)
			{
				weeklyEventTypes.Add(WeeklyEventType.Most_damage_bounced);
				WeeklyEvent.WeeklyEventItems[3] = new WeeklyEventItem(battle.details.damage_blocked, battle.player_name, battle.view_url, weeklyEventTypes[weeklyEventTypes.Count - 1]);
			}
			if (WeeklyEvent.WeeklyEventItems[4].Value < battle.details.damage_assisted + battle.details.damage_assisted_track)
			{
				weeklyEventTypes.Add(WeeklyEventType.Most_assist_damage);
				WeeklyEvent.WeeklyEventItems[4] = new WeeklyEventItem(battle.details.damage_assisted + battle.details.damage_assisted_track, battle.player_name, battle.view_url, weeklyEventTypes[weeklyEventTypes.Count - 1]);
			}
			if (WeeklyEvent.WeeklyEventItems[5].Value < battle.details.enemies_destroyed)
			{
				weeklyEventTypes.Add(WeeklyEventType.Most_destroyed);
				WeeklyEvent.WeeklyEventItems[5] = new WeeklyEventItem(battle.details.enemies_destroyed, battle.player_name, battle.view_url, weeklyEventTypes[weeklyEventTypes.Count - 1]);
			}
			if (WeeklyEvent.WeeklyEventItems[6].Value < battle.details.shots_pen)
			{
				weeklyEventTypes.Add(WeeklyEventType.Most_hits);
				WeeklyEvent.WeeklyEventItems[6] = new WeeklyEventItem(battle.details.shots_pen, battle.player_name, battle.view_url, weeklyEventTypes[weeklyEventTypes.Count - 1]);
			}
			await UpdateLastWeeklyEvent();
		}
		return weeklyEventTypes;
	}
	public async Task<string> GetStringForWeeklyEvent(WGBattle battle)
	{
		string content = string.Empty;
		await ReadWeeklyEvent();
		if (WeeklyEvent != null && WeeklyEvent.Tank == battle.vehicle && DiscordMessage != null)
		{
			if (battle.room_type is 1 or 5 or 7 or 4)
			{
				if (battle.battle_start_time.HasValue && WeeklyEvent.StartDate < battle.battle_start_time.Value && WeeklyEvent.StartDate.AddDays(7) > battle.battle_start_time.Value)
				{
					List<WeeklyEventType> weeklyEventTypes = await CheckAndHandleWeeklyEvent(battle);
					if (weeklyEventTypes.Count > 0)
					{
						if (weeklyEventTypes.Count > 1)
						{
							StringBuilder sb = new();
							sb.Append("Proficiat, je hebt de beste score voor meerdere onderdelen van het wekelijkse event:\n");
							foreach (WeeklyEventType weeklyEventType in weeklyEventTypes)
							{
								sb.Append("• ");
								sb.Append(weeklyEventType.ToString().Replace('_', ' '));
								sb.Append('\n');
							}
							content = sb.ToString();
						}
						else
						{
							content = "Proficiat, je hebt de beste score voor `" + weeklyEventTypes[0].ToString().Replace('_', ' ').ToLower() + "` van het wekelijkse event.";
						}
					}
				}
			}
		}
		return content + (content.Length > 0 ? Environment.NewLine : string.Empty);
	}
	public async Task ReadWeeklyEvent()
	{
		try
		{
			DiscordChannel weeklyEventChannel = await _channelService.GetWeeklyEventChannel();
			if (weeklyEventChannel != null)
			{
				IReadOnlyList<DiscordMessage> msgs = weeklyEventChannel.GetMessagesAsync(1).Result;
				if (msgs.Count > 0)
				{
					//hier lastmessage bij dm
					DiscordMessage message = msgs[0];
					if (message != null)
					{
						DiscordMessage = message;
						WeeklyEvent = new WeeklyEvent(message);
					}
					else
					{
						await _errorHandler.HandleErrorAsync("The last DiscordMessage in weeklyEventChannel was null while executing ReadWeeklyEvent method.");
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (ex.Message != "Not found: 404")
			{
				await _errorHandler.HandleErrorAsync("Something went wrong at ReadWeeklyEvent:", ex);
			}
		}
	}

	public async Task UpdateLastWeeklyEvent()
	{
		try
		{
			await DiscordMessage.ModifyAsync(WeeklyEvent.GenerateEmbed());
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("While editing HOF message: ", ex);
		}
	}

	public async Task CreateNewWeeklyEvent(string tank, DiscordChannel weeklyEventChannel)
	{
		List<WeeklyEventItem> weeklyEventItems = [];

		foreach (WeeklyEventType type in Enum.GetValues<WeeklyEventType>())
		{
			weeklyEventItems.Add(new WeeklyEventItem(type));
		}

		WeeklyEvent weeklyEvent = new(tank, weeklyEventItems);

		try
		{
			await weeklyEventChannel.SendMessageAsync(embed: weeklyEvent.GenerateEmbed());
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("While editing HOF message: ", ex);
		}
	}
}
