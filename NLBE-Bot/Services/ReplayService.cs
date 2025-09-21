namespace NLBE_Bot.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
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
							 IVehiclesRepository vehiclesRepository,
							 IClansRepository clanRepository,
							 IMapService mapService,
							 IAttachmentService attachmentService,
							 IMemoryCache cache) : IReplayService
{
	private const string AllAchievementsCacheKey = "AllAchievements";

	private readonly ILogger<ReplayService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IWeeklyEventService _weeklyEventHandler = weeklyEventHandler ?? throw new ArgumentNullException(nameof(weeklyEventHandler));
	private readonly IBattleRepository _battleRepository = battleRepository ?? throw new ArgumentNullException(nameof(battleRepository));
	private readonly IAchievementsRepository _achievementsRepository = achievementsRepository ?? throw new ArgumentNullException(nameof(achievementsRepository));
	private readonly IVehiclesRepository _vehiclesRepository = vehiclesRepository ?? throw new ArgumentNullException(nameof(vehiclesRepository));
	private readonly IClansRepository _clanRepository = clanRepository ?? throw new ArgumentNullException(nameof(clanRepository));
	private readonly IMapService _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
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

		string replayInfo = await GetSomeReplayInfoAsText(guild, battle, position);
		sb.Append(replayInfo.Replace(Constants.REPLACEABLE_UNDERSCORE_CHAR, '_'));
		return sb.ToString();
	}

	public async Task<WotInspectorBattle?> GetReplayInfo(string title, IDiscordAttachment attachment)
	{
		(string fileName, byte[] fileContent) = await _attachmentService.DownloadAttachmentAsync(attachment);
		return await _battleRepository.GetBattle(fileName, fileContent, title);
	}

	private async Task<string> GetSomeReplayInfoAsText(IDiscordGuild guild, WotInspectorBattle battle, int position)
	{
		StringBuilder sb = new();

		if (Guard.ReturnIfNull(battle, _logger, "battle data of the replay", out battle) ||
			Guard.ReturnIfNull(battle.ProtagonistPlayerData, _logger, "protagonist's player data", out WotInspectorPlayerData protagonistPlayerData) ||
			Guard.ReturnIfNull(await _vehiclesRepository.GetById(battle.VehicleDescr), _logger, "vehicle information", out WotbVehicle vehicle) ||
			Guard.ReturnIfNull(await _clanRepository.GetAccountClanInfoAsync(battle.Protagonist), _logger, "account's clan information", out WotbAccountClanInfo accountClanInfo))
		{
			return string.Empty;
		}

		Dictionary<string, MapInfo> maps = await _mapService.GetAllMaps(guild);
		maps.TryGetValue(battle.MapId.ToString(), out MapInfo? map);

		sb.AppendLine(GetInfoInFormat("Link", "[" + battle.Title.AdaptToChat().Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR) + "](" + battle.DetailsUrl.AdaptToChat() + ")", false));
		sb.AppendLine(GetInfoInFormat("Player", battle.PlayerName.AdaptToChat()));
		sb.AppendLine(GetInfoInFormat("Clan", accountClanInfo?.Clan.Tag ?? string.Empty));
		sb.AppendLine(GetInfoInFormat("Vehicle", vehicle.Name));
		sb.AppendLine(GetInfoInFormat("Tier", Emoj.GetName(vehicle.Tier), false));
		sb.AppendLine(GetInfoInFormat("Damage", battle.DamageMade.ToString()));
		sb.AppendLine(GetInfoInFormat("Damage assisted (total)", protagonistPlayerData.DamageAssistedCombined.ToString()));
		sb.AppendLine(GetInfoInFormat("Damage blocked", protagonistPlayerData.DamageBlocked.ToString()));
		sb.AppendLine(GetInfoInFormat("Exp. base", battle.ExpBase.ToString()));
		sb.AppendLine(GetInfoInFormat("Shots penned", protagonistPlayerData.ShotsPen.ToString()));
		sb.AppendLine(GetInfoInFormat("Enemies destroyed", protagonistPlayerData.EnemiesDestroyed.ToString()));
		sb.AppendLine(GetInfoInFormat("Map", map?.Name ?? string.Empty));
		sb.AppendLine(GetInfoInFormat("Type", battle.BattleTypeAsString));
		sb.AppendLine(GetInfoInFormat("Mode", battle.RoomTypeAsString));
		sb.AppendLine(GetInfoInFormat("Result", battle.BattleResultAsString));
		sb.AppendLine(GetInfoInFormat("Start Time", battle.BattleStartTime.ToString("dd-MM-yyyy HH:mm:ss")));

		if (position > 0)
		{
			sb.AppendLine(GetInfoInFormat("Hall of Fame position", position.ToString()));
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
