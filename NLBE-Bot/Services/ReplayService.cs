namespace NLBE_Bot.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

internal class ReplayService(ILogger<ReplayService> logger,
					 		 IOptions<BotOptions> options,
						 	 IWeeklyEventService weeklyEventHandler,
							 IAccountsRepository accountRepository,
							 IBattleRepository battleRepository,
							 IAttachmentService attachmentService) : IReplayService
{
	private readonly ILogger<ReplayService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly IWeeklyEventService _weeklyEventHandler = weeklyEventHandler ?? throw new ArgumentNullException(nameof(weeklyEventHandler));
	private readonly IAccountsRepository _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
	private readonly IBattleRepository _battleRepository = battleRepository ?? throw new ArgumentNullException(nameof(battleRepository));
	private readonly IAttachmentService _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));

	public async Task<string> GetDescriptionForReplay(IDiscordGuild guild, WotbBattle battle, int position, string preDescription = "")
	{
		StringBuilder sb = new(preDescription);

		try
		{
			string weeklyEventDescription = await _weeklyEventHandler.GetStringForWeeklyEvent(guild, battle);

			if (weeklyEventDescription.Length > 0)
			{
				sb.AppendLine(weeklyEventDescription);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while getting weekly event description for replay.");
		}

		string replayInfo = await GetSomeReplayInfoAsText(battle, position);
		sb.Append(replayInfo.Replace(Constants.REPLACEABLE_UNDERSCORE_CHAR, '_'));
		return sb.ToString();
	}

	public async Task<WotbBattle?> GetReplayInfo(string title, IDiscordAttachment attachment, string playerName)
	{
		IReadOnlyList<WotbAccountListItem> accountInfo = await _accountRepository.SearchByNameAsync(SearchType.Exact, playerName);

		long? accountId = accountInfo.Count > 0 ? accountInfo[0].AccountId : null;
		(string fileName, byte[] fileContent) = await _attachmentService.DownloadAttachmentAsync(attachment);
		return await _battleRepository.GetBattle(fileName, fileContent, title, accountId);
	}

	private async Task<string> GetSomeReplayInfoAsText(WotbBattle battle, int position)
	{
		StringBuilder sb = new();
		WotbBattleSummary summary = battle.Summary;

		sb.AppendLine(GetInfoInFormat("Link", "[" + summary.Title.AdaptToChat().Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR) + "](" + battle.ViewUrl.AdaptToChat() + ")", false));
		sb.AppendLine(GetInfoInFormat("Speler", summary.PlayerName.AdaptToChat()));
		sb.AppendLine(GetInfoInFormat("Clan", summary.Details.clan_tag));
		sb.AppendLine(GetInfoInFormat("Tank", summary.Vehicle));
		sb.AppendLine(GetInfoInFormat("Tier", Emoj.GetName(summary.VehicleTier), false));
		sb.AppendLine(GetInfoInFormat("Damage", summary.Details.damage_made.ToString()));
		sb.AppendLine(GetInfoInFormat("Damage bounced", summary.Details.damage_blocked.ToString()));
		sb.AppendLine(GetInfoInFormat("Assist damage", (summary.Details.damage_assisted + summary.Details.damage_assisted_track).ToString()));
		sb.AppendLine(GetInfoInFormat("exp", summary.Details.exp.ToString()));
		sb.AppendLine(GetInfoInFormat("Hits", summary.Details.shots_pen.ToString()));
		sb.AppendLine(GetInfoInFormat("Tanks vernietigd", summary.Details.enemies_destroyed.ToString()));
		sb.AppendLine(GetInfoInFormat("Map", summary.MapName));
		string resultaat = "Gewonnen";
		if (summary.ProtagonistTeam != summary.WinnerTeam)
		{
			resultaat = summary.WinnerTeam is not 2 and not 1 ? "Gelijk gespeeld" : "Verloren";
		}
		sb.AppendLine(GetInfoInFormat("Resultaat", resultaat));
		if (summary.BattleStartTime.HasValue)
		{
			sb.AppendLine(GetInfoInFormat("Datum", (summary.BattleStartTime.Value.Day < 10 ? "0" : string.Empty) + summary.BattleStartTime.Value.Day + "-" + summary.BattleStartTime.Value.Month + "-" + summary.BattleStartTime.Value.Year + " " + summary.BattleStartTime.Value.Hour + ":" + (summary.BattleStartTime.Value.Minute < 10 ? "0" : string.Empty) + summary.BattleStartTime.Value.Minute + ":" + (summary.BattleStartTime.Value.Second < 10 ? "0" : string.Empty) + summary.BattleStartTime.Value.Second));
		}
		sb.AppendLine(GetInfoInFormat("Type", WotbBattleSummary.GetBattleType(summary.BattleType)));
		sb.AppendLine(GetInfoInFormat("Mode", WotbBattleSummary.GetBattleRoom(summary.RoomType)));
		if (position > 0)
		{
			sb.AppendLine(GetInfoInFormat("Positie in HOF", position.ToString()));
		}
		if (summary.Details.achievements != null && summary.Details.achievements.Count > 0)
		{
			List<Achievement> achievementList = [];
			for (int i = 0; i < summary.Details.achievements.Count; i++)
			{
				Achievement tempAchievement = await Achievement.getAchievement(_options.WotbApi.ApplicationId, summary.Details.achievements.ElementAt(i).t);
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
				foreach (Achievement tempAchievement in achievementList)
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
		if (!string.IsNullOrEmpty(value) && bold)
		{
			value = $"**{value}**";
		}

		return $"{key}: {value}";
	}
}
