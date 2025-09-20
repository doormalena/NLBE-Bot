namespace NLBE_Bot.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

internal class ReplayService(ILogger<ReplayService> logger,
						 	 IWeeklyEventService weeklyEventHandler,
							 IBattleRepository battleRepository,
							 IAchievementsRepository achievementsRepository,
							 IAttachmentService attachmentService,
							 IMemoryCache cache) : IReplayService
{
	private const string AllAchievementsCacheKey = "AllAchievements";
	private readonly ILogger<ReplayService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IWeeklyEventService _weeklyEventHandler = weeklyEventHandler ?? throw new ArgumentNullException(nameof(weeklyEventHandler));
	private readonly IBattleRepository _battleRepository = battleRepository ?? throw new ArgumentNullException(nameof(battleRepository));
	private readonly IAchievementsRepository _achievementsRepository = achievementsRepository ?? throw new ArgumentNullException(nameof(achievementsRepository));
	private readonly IAttachmentService _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
	private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

	public async Task<string> GetDescriptionForReplay(IDiscordGuild guild, WotInspectorBattle battle, int position, string preDescription = "")
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

	public async Task<WotInspectorBattle?> GetReplayInfo(string title, IDiscordAttachment attachment)
	{
		(string fileName, byte[] fileContent) = await _attachmentService.DownloadAttachmentAsync(attachment);
		return await _battleRepository.GetBattle(fileName, fileContent, title);
	}

	private async Task<string> GetSomeReplayInfoAsText(WotInspectorBattle battle, int position)
	{
		StringBuilder sb = new();

		if (battle == null)
		{
			return "Geen replay info gevonden.";
		}

		WotInspectorPlayerData? protagonistPlayerData = battle.ProtagonistPlayerData;

		if (protagonistPlayerData == null)
		{
			return "The player data could not be found in the replay data.";
		}

		sb.AppendLine(GetInfoInFormat("Link", "[" + battle.Title.AdaptToChat().Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR) + "](" + battle.DetailsUrl.AdaptToChat() + ")", false));
		sb.AppendLine(GetInfoInFormat("Speler", battle.PlayerName.AdaptToChat()));
		sb.AppendLine(GetInfoInFormat("Clan", battle.ProtagonistClan.ToString())); // TODO: covert to name using clan repository
		sb.AppendLine(GetInfoInFormat("Tank", battle.VehicleDescr.ToString())); // TODO: covert to name using Wotb API?
		//sb.AppendLine(GetInfoInFormat("Tier", Emoj.GetName(battle.VehicleTier), false)); // TODO: get from verhicle using Wotb API?
		sb.AppendLine(GetInfoInFormat("Damage", battle.DamageMade.ToString()));
		sb.AppendLine(GetInfoInFormat("Damage assisted (total)", protagonistPlayerData.DamageAssistedCombined.ToString()));
		sb.AppendLine(GetInfoInFormat("Damage blocked", protagonistPlayerData.DamageBlocked.ToString()));
		sb.AppendLine(GetInfoInFormat("Exp (total)", battle.ExpTotal.ToString()));
		sb.AppendLine(GetInfoInFormat("Hits", protagonistPlayerData.ShotsPen.ToString()));
		sb.AppendLine(GetInfoInFormat("Tanks vernietigd", protagonistPlayerData.EnemiesDestroyed.ToString()));
		sb.AppendLine(GetInfoInFormat("Map", battle.MapId.ToString())); // TODO: covert to name using Wotb API?
		sb.AppendLine(GetInfoInFormat("Resultaat", battle.BattleResultAsString));
		sb.AppendLine(GetInfoInFormat("Datum", battle.BattleStartTime.ToString("dd-MM-yyyy HH:mm:ss")));
		sb.AppendLine(GetInfoInFormat("Type", battle.BattleTypeAsString));
		sb.AppendLine(GetInfoInFormat("Mode", battle.RoomTypeAsString));

		if (position > 0)
		{
			sb.AppendLine(GetInfoInFormat("Positie in HOF", position.ToString()));
		}

		if (protagonistPlayerData.Achievements != null && protagonistPlayerData.Achievements.Count > 0)
		{
			List<WotbAchievement> achievementList = [];

			for (int i = 0; i < protagonistPlayerData.Achievements.Count; i++)
			{
				KeyValuePair<string, int> a = protagonistPlayerData.Achievements.ElementAt(i);
				int.TryParse(a.Key, out int achievementId);

				WotbAchievement? tempAchievement = await GetAchievement(achievementId);

				if (tempAchievement != null)
				{
					achievementList.Add(tempAchievement);
				}
			}
			if (achievementList.Count > 0)
			{
				achievementList = achievementList.OrderBy(x => x.Order).ToList();
				sb.AppendLine("Achievements:");
				sb.Append("```");
				foreach (WotbAchievement tempAchievement in achievementList)
				{
					sb.AppendLine(tempAchievement.Name.Replace("\n", string.Empty).Replace("(" + tempAchievement.AchievementId + ")", string.Empty));
				}
				sb.Append("```");
			}
		}
		return sb.ToString();
	}

	private async Task<WotbAchievement?> GetAchievement(int id)
	{
		Dictionary<string, WotbAchievement>? achievements = await _cache.GetOrCreateAsync(AllAchievementsCacheKey, entry => _achievementsRepository.GetAllAsync());

		if (achievements == null)
		{
			return null;
		}

		return achievements.FirstOrDefault(x => x.Value.WotInspectorId == id).Value;
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
