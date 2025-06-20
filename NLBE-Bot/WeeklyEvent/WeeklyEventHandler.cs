namespace NLBE_Bot;

using DSharpPlus.Entities;
using FMWOTB.Tools.Replays;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

internal class WeeklyEventHandler(IErrorHandler errorHandler, IChannelService channelService) : IWeeklyEventHandler
{
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));

	public DiscordMessage DiscordMessage
	{
		get; set;
	} //The last message in Weekly events
	public WeeklyEvent WeeklyEvent
	{
		get; set;
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
