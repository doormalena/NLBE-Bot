namespace NLBE_Bot.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tools.Replays;

internal class WeeklyEventService(IChannelService channelService,
								  IUserService userService,
								  IBotState botState,
								  ILogger<WeeklyEventService> _logger,
								  IOptions<BotOptions> options) : IWeeklyEventService
{
	private readonly ILogger<WeeklyEventService> _logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

	public IDiscordMessage DiscordMessage
	{
		get; set;
	} //The last message in Weekly events

	public WeeklyEvent WeeklyEvent
	{
		get; set;
	}

	public async Task WeHaveAWinner(IDiscordGuild guild, WeeklyEventItem weeklyEventItemMostDMG, string tank)
	{
		if (Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.BotTest), _logger, "Bot Test channel", out IDiscordChannel bottestChannel) ||
			Guard.ReturnIfNull(await _channelService.GetAlgemeenChannelAsync(), _logger, "General channel", out IDiscordChannel generalChannel))
		{
			return;
		}

		bool userNotFound = true;
		IReadOnlyCollection<IDiscordMember> members = await guild.GetAllMembersAsync();

		if (weeklyEventItemMostDMG.Player != null)
		{
			string weeklyEventItemMostDMGPlayer = weeklyEventItemMostDMG.Player
				.Replace("\\", string.Empty)
				.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_')
				.ToLower();

			foreach (IDiscordMember member in members)
			{
				if (!member.IsBot)
				{
					WotbPlayerNameInfo playerInfo = _userService.GetWotbPlayerNameFromDisplayName(member.DisplayName);
					string x = playerInfo.PlayerName
						.Replace("\\", string.Empty)
						.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_')
						.ToLower();

					if (x == weeklyEventItemMostDMGPlayer)
					{
						userNotFound = false;

						_botState.WeeklyEventWinner = new WeeklyEventWinner { UserId = member.Id, LastEventDate = DateTime.Now };

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
							_logger.LogError(ex, "Could not send private message to weekly event winner {PlayerName}.", member.DisplayName);
						}
						try
						{
							await generalChannel.SendMessageAsync("Feliciteer **" + weeklyEventItemMostDMG.Player.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_').AdaptToChat() + "** want hij heeft het wekelijkse event gewonnen! **Proficiat!**" +
																	"\n" +
																	"`" + tank + "` met `" + weeklyEventItemMostDMG.Value + "` damage" +
																	"\n\n" +
																	"We wachten nu af tot de winnaar een nieuwe tank kiest.");
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Could not send message in algemeen channel for weekly event winner announcement.");
						}
						break;
					}
				}
			}
		}
		else
		{
			await generalChannel.SendMessageAsync("Het wekelijkse event is gedaan, helaas heeft er __niemand__ deelgenomen en is er dus geen winnaar.");
		}

		if (userNotFound)
		{
			string message = "Een weekly event winnaar was niet gevonden. Je zal handmatig een nieuw weekly event moeten aanmaken middels het `weekly` commando.";
			await bottestChannel.SendMessageAsync(message);

			_logger.LogWarning("A weekly event winner was not found. You will have setup a new weeky event using the `weekly` command.");
		}
		else
		{
			string message = "Weekly event winnaar gevonden!";
			await bottestChannel.SendMessageAsync(message);

			_logger.LogInformation("Weekly event winner found and notified.");
		}
	}

	public async Task<List<WeeklyEventType>> CheckAndHandleWeeklyEvent(WGBattle battle)
	{
		List<WeeklyEventType> weeklyEventTypes = [];

		if (battle.vehicle == WeeklyEvent.Tank)
		{
			//TODO: refactor into a switch statement

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

		if (WeeklyEvent != null && WeeklyEvent.Tank == battle.vehicle && DiscordMessage != null && battle.room_type is 1 or 5 or 7 or 4 && battle.battle_start_time.HasValue && WeeklyEvent.StartDate < battle.battle_start_time.Value && WeeklyEvent.StartDate.AddDays(7) > battle.battle_start_time.Value)
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

		return content + (content.Length > 0 ? Environment.NewLine : string.Empty);
	}

	public async Task ReadWeeklyEvent()
	{
		try
		{
			IDiscordChannel weeklyEventChannel = await _channelService.GetWeeklyEventChannelAsync();

			if (weeklyEventChannel != null)
			{
				IReadOnlyList<IDiscordMessage> msgs = weeklyEventChannel.GetMessagesAsync(1).Result;

				if (msgs.Count > 0)
				{
					//hier lastmessage bij dm
					IDiscordMessage message = msgs[0];

					if (message != null)
					{
						DiscordMessage = message;
						WeeklyEvent = new WeeklyEvent(message);
					}
					else
					{
						_logger.LogError("The last DiscordMessage in weeklyEventChannel was null while executing ReadWeeklyEvent method.");
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (ex.Message != "Not found: 404")
			{
				_logger.LogError(ex, "Error while reading the last message in weekly event channel.");
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
			_logger.LogError(ex, "Error while updating the last message in weekly event channel.");
		}
	}

	public async Task CreateNewWeeklyEvent(string tank, IDiscordChannel weeklyEventChannel)
	{
		List<WeeklyEventItem> weeklyEventItems = [];

		foreach (WeeklyEventType type in Enum.GetValues<WeeklyEventType>())
		{
			weeklyEventItems.Add(new WeeklyEventItem(type));
		}

		WeeklyEvent weeklyEvent = new(tank, weeklyEventItems);

		try
		{
			await weeklyEventChannel.SendMessageAsync(weeklyEvent.GenerateEmbed());
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while sending new weekly event message to the weekly event channel.");
		}
	}
}
