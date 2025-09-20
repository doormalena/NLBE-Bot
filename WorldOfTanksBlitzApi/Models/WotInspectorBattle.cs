namespace WorldOfTanksBlitzApi.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

public class WotInspectorBattle
{
	[JsonInclude, JsonPropertyName("id")]
	public string Id { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("map_id")]
	public int MapId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("battle_duration")]
	public double BattleDuration
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("title")]
	public string Title { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("player_name")]
	public string PlayerName { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("protagonist")]
	public long Protagonist
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("vehicle_descr")]
	public int VehicleDescr
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("mastery_badge")]
	public int MasteryBadge
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp_base")]
	public int ExpBase
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("enemies_spotted")]
	public int EnemiesSpotted
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("enemies_destroyed")]
	public int EnemiesDestroyed
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("damage_assisted")]
	public int DamageAssisted
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("damage_made")]
	public int DamageMade
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("details_url")]
	public string DetailsUrl { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("download_url")]
	public string DownloadUrl { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("game_version")]
	public GameVersionInfo GameVersion { get; internal set; } = new();

	[JsonInclude, JsonPropertyName("arena_unique_id")]
	public string ArenaUniqueId { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("download_count")]
	public int DownloadCount
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("data_version")]
	public int DataVersion
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("private")]
	public bool Private
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("private_clan")]
	public bool PrivateClan
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("battle_start_time")]
	public DateTime BattleStartTime
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("upload_time")]
	public DateTime UploadTime
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("allies")]
	public List<long> Allies { get; internal set; } = [];

	[JsonInclude, JsonPropertyName("enemies")]
	public List<long> Enemies { get; internal set; } = [];

	[JsonInclude, JsonPropertyName("protagonist_clan")]
	public long ProtagonistClan
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("protagonist_team")]
	public int ProtagonistTeam
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("battle_result")]
	public int BattleResult
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("credits_base")]
	public int CreditsBase
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("tags")]
	public List<int> Tags { get; internal set; } = [];

	[JsonInclude, JsonPropertyName("battle_type")]
	public int BattleType
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("room_type")]
	public int RoomType
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("winner_team")]
	public int WinnerTeam
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("finish_reason")]
	public int FinishReason
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("players_data")]
	public List<WotInspectorPlayerData> PlayersData { get; internal set; } = [];

	[JsonInclude, JsonPropertyName("exp_total")]
	public int ExpTotal
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("credits_total")]
	public int CreditsTotal
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("repair_cost")]
	public int RepairCost
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp_free")]
	public int ExpFree
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp_free_base")]
	public int ExpFreeBase
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp_penalty")]
	public int ExpPenalty
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("credits_penalty")]
	public int CreditsPenalty
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("credits_contribution_in")]
	public int CreditsContributionIn
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("credits_contribution_out")]
	public int CreditsContributionOut
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("camouflage_id")]
	public int CamouflageId
	{
		get; internal set;
	}

	public string BattleTypeAsString => BattleType switch
	{
		0 => "Encounter",
		1 => "Supremacy",
		_ => string.Empty,
	};

	public string RoomTypeAsString => RoomType switch
	{
		1 => "Normal",
		2 => "Training",
		4 or 5 => "Tournament",
		7 => "Rating",
		8 => "Mad Games",
		22 => "Realistic",
		23 => "Uprising",
		24 => "Gravity Force",
		25 => "Skirmish",
		26 => "Burning",
		27 => "Boss Fight",
		_ => string.Empty
	};

	public string BattleResultAsString => BattleResult switch
	{
		1 => "Victory",
		2 => "Defeat",
		_ => "Draw"
	};

	public WotInspectorPlayerData? ProtagonistPlayerData => PlayersData != null && Protagonist > 0
			? PlayersData.FirstOrDefault(p => p.DbId == Protagonist)
			: null;
}
