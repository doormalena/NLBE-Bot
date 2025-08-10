namespace NLBE_Bot.Services;

using DiscordHelper;
using DSharpPlus.Entities;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Account;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;
using WorldOfTanksBlitzApi.Tools.Replays;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

internal class ReplayService(ILogger<ReplayService> logger,
					 		 IOptions<BotOptions> options,
						 	 IWeeklyEventService weeklyEventHandler,
							 IAccountsRepository accountRepository) : IReplayService
{
	private readonly ILogger<ReplayService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly IWeeklyEventService _weeklyEventHandler = weeklyEventHandler ?? throw new ArgumentNullException(nameof(weeklyEventHandler));
	private readonly IAccountsRepository _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));

	public async Task<string> GetDescriptionForReplay(WGBattle battle, int position, string preDescription = "")
	{
		StringBuilder sb = new(preDescription);
		try
		{
			string weeklyEventDescription = await _weeklyEventHandler.GetStringForWeeklyEvent(battle);
			if (weeklyEventDescription.Length > 0)
			{
				sb.Append(Environment.NewLine + weeklyEventDescription);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while getting weekly event description for replay.");
		}
		sb.Append(GetSomeReplayInfoAsText(battle, position).Replace(Constants.REPLACEABLE_UNDERSCORE_CHAR, '_'));
		return sb.ToString();
	}

	public async Task<WGBattle> GetReplayInfo(string titel, object attachment, string ign, string url)
	{
		string json = string.Empty;
		bool playerIDFound = false;

		IReadOnlyList<WotbAccountListItem> accountInfo = await _accountRepository.SearchByNameAsync(SearchType.Exact, ign);

		if (accountInfo != null && accountInfo.Count > 0)
		{
			playerIDFound = true;
			if (attachment != null)
			{
				DiscordAttachment attach = (DiscordAttachment) attachment;
				url = attach.Url;
			}
			json = await ReplayToString(url, titel, accountInfo[0].AccountId);
		}
		if (!playerIDFound)
		{
			if (attachment != null)
			{
				DiscordAttachment attach = (DiscordAttachment) attachment;
				url = attach.Url;
			}
			json = await ReplayToString(url, titel, null);
		}
		try
		{
			return new WGBattle(json);
		}
		catch (Exception ex)
		{
			string attachUrl = "Nothing";
			if (attachment != null)
			{
				DiscordAttachment attach = (DiscordAttachment) attachment;
				attachUrl = attach.Url;
			}

			_logger.LogError(ex, "Error while initializing WGBattle object from ({Url}): {Json}", !string.IsNullOrEmpty(url) ? url : attachUrl, json);
		}
		return null;
	}
	private static async Task<string> ReplayToString(string pathOrKey, string title, long? wg_id)
	{
		string url = @"https://wotinspector.com/api/replay/upload?url=";
		HttpClient httpClient = new();
		MultipartFormDataContent form1 = [];
		string AsBase64String = null;
		if (pathOrKey.StartsWith("http"))
		{
			if (pathOrKey.Contains("wotinspector"))
			{
				//return een json in deze else
				HttpResponseMessage iets = await httpClient.GetAsync("https://api.wotinspector.com/replay/upload?details=full&key=" + Path.GetFileName(pathOrKey));
				if (iets != null && iets.Content != null)
				{
					return await iets.Content.ReadAsStringAsync();
				}
				return null;
			}
			else
			{
				AsBase64String = Convert.ToBase64String(await httpClient.GetByteArrayAsync(pathOrKey));
			}
		}
		else if (pathOrKey.Contains('\\') || pathOrKey.Contains('/'))
		{
			AsBase64String = Convert.ToBase64String(await File.ReadAllBytesAsync(pathOrKey));
		}

		form1.Add(new StringContent(Path.GetFileName(pathOrKey)), "filename");
		form1.Add(new StringContent(AsBase64String), "file");
		if (!string.IsNullOrEmpty(title))
		{
			form1.Add(new StringContent(title), "title");
		}
		if (wg_id != null)
		{
			form1.Add(new StringContent(wg_id.ToString()), "loaded_by");
		}

		HttpResponseMessage response = await httpClient.PostAsync(url, form1);
		return await response.Content.ReadAsStringAsync();
	}
	private string GetSomeReplayInfoAsText(WGBattle battle, int position)
	{
		StringBuilder sb = new();
		sb.AppendLine(GetInfoInFormat("Link", "[" + battle.title.AdaptToDiscordChat().Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR) + "](" + battle.view_url.AdaptToDiscordChat() + ")", false));
		sb.AppendLine(GetInfoInFormat("Speler", battle.player_name.AdaptToDiscordChat()));
		sb.AppendLine(GetInfoInFormat("Clan", battle.details.clan_tag));
		sb.AppendLine(GetInfoInFormat("Tank", battle.vehicle));
		sb.AppendLine(GetInfoInFormat("Tier", Emoj.GetName(battle.vehicle_tier), false));
		sb.AppendLine(GetInfoInFormat("Damage", battle.details.damage_made.ToString()));
		sb.AppendLine(GetInfoInFormat("Damage bounced", battle.details.damage_blocked.ToString()));
		sb.AppendLine(GetInfoInFormat("Assist damage", (battle.details.damage_assisted + battle.details.damage_assisted_track).ToString()));
		sb.AppendLine(GetInfoInFormat("exp", battle.details.exp.ToString()));
		sb.AppendLine(GetInfoInFormat("Hits", battle.details.shots_pen.ToString()));
		sb.AppendLine(GetInfoInFormat("Tanks vernietigd", battle.details.enemies_destroyed.ToString()));
		sb.AppendLine(GetInfoInFormat("Map", battle.map_name));
		string resultaat = "Gewonnen";
		if (battle.protagonist_team != battle.winner_team)
		{
			resultaat = battle.winner_team is not 2 and not 1 ? "Gelijk gespeeld" : "Verloren";
		}
		sb.AppendLine(GetInfoInFormat("Resultaat", resultaat));
		if (battle.battle_start_time.HasValue)
		{
			sb.AppendLine(GetInfoInFormat("Datum", (battle.battle_start_time.Value.Day < 10 ? "0" : string.Empty) + battle.battle_start_time.Value.Day + "-" + battle.battle_start_time.Value.Month + "-" + battle.battle_start_time.Value.Year + " " + battle.battle_start_time.Value.Hour + ":" + (battle.battle_start_time.Value.Minute < 10 ? "0" : string.Empty) + battle.battle_start_time.Value.Minute + ":" + (battle.battle_start_time.Value.Second < 10 ? "0" : string.Empty) + battle.battle_start_time.Value.Second));
		}
		sb.AppendLine(GetInfoInFormat("Type", WGBattle.getBattleType(battle.battle_type)));
		sb.AppendLine(GetInfoInFormat("Mode", WGBattle.getBattleRoom(battle.room_type)));
		if (position > 0)
		{
			sb.AppendLine(GetInfoInFormat("Positie in HOF", position.ToString()));
		}
		if (battle.details.achievements != null && battle.details.achievements.Count > 0)
		{
			List<WorldOfTanksBlitzApi.Achievement> achievementList = [];
			for (int i = 0; i < battle.details.achievements.Count; i++)
			{
				WorldOfTanksBlitzApi.Achievement tempAchievement = WorldOfTanksBlitzApi.Achievement.getAchievement(_options.WotbApi.ApplicationId, battle.details.achievements.ElementAt(i).t).Result;
				if (tempAchievement != null)
				{
					achievementList.Add(tempAchievement);
				}
			}
			if (achievementList.Count > 0)
			{
				achievementList = achievementList.OrderBy(x => x.order).ToList();
				sb.AppendLine("Achievements:");
				sb.Append("```");
				foreach (WorldOfTanksBlitzApi.Achievement tempAchievement in achievementList)
				{
					sb.AppendLine(tempAchievement.name.Replace("\n", string.Empty).Replace("(" + tempAchievement.achievement_id + ")", string.Empty));
				}
				sb.Append("```");
			}
		}
		return sb.ToString();
	}

	private static string GetInfoInFormat(string key, string value, bool bold = true)
	{
		if (value != null)
		{
			if (bold)
			{
				if (value != string.Empty)
				{
					value = "**" + value + "**";
				}
			}
		}
		return key + ": " + (value ?? string.Empty);
	}
}
