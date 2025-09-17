namespace WorldOfTanksBlitzApi.Models;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotbBattleSummary
{
	[JsonInclude, JsonPropertyName("download_url")]
	public string DownloadUrl
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("winner_team")]
	public int WinnerTeam
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("uploaded_by")]
	public long UploadedBy
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("credits_total")]
	public int CreditsTotal
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp_base")]
	public int ExpBase
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("player_name")]
	public string PlayerName
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("title")]
	public string Title
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("details")]
	public WotbBattleDetails Details
	{
		get; internal set;
	} = new();

	[JsonInclude, JsonPropertyName("fullDetails")]
	public List<WotbBattleDetails> FullDetails
	{
		get; internal set;
	} = [];

	[JsonInclude, JsonPropertyName("vehicle")]
	public string Vehicle
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("enemies")]
	public List<long> Enemies
	{
		get; internal set;
	} = [];

	[JsonInclude, JsonPropertyName("allies")]
	public List<long> Allies
	{
		get; internal set;
	} = [];

	[JsonInclude, JsonPropertyName("description")]
	public string Description
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("battle_duration")]
	public double BattleDuration
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("arena_unique_id")]
	public ulong ArenaUniqueId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("vehicle_tier")]
	public int VehicleTier
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("battle_start_time")]
	public DateTime? BattleStartTime
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("mastery_badge")]
	public int MasteryBadge
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("protagonist")]
	public long Protagonist
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("battle_type")]
	public int BattleType
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp_total")]
	public int ExpTotal
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("vehicle_type")]
	public int VehicleType
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("battle_start_timestamp")]
	public double BattleStartTimestamp
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("credits_base")]
	public int CreditsBase
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("protagonist_team")]
	public int ProtagonistTeam
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("map_name")]
	public string MapName
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("room_type")]
	public int RoomType
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("battle_result")]
	public int BattleResult
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("error")]
	public string Error
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("hexKey")]
	public string HexKey
	{
		get; internal set;
	} = string.Empty;

	public string ViewOnline => !string.IsNullOrEmpty(HexKey)
				? "https://map.wotinspector.com/en/?url=https://replays.wotinspector.com/en/download/" + HexKey + "&frame&package=blitz8.2&platform=blitz"
				: string.Empty;

	public static string GetBattleType(int type)
	{
		return type switch
		{
			0 => "encounter",
			1 => "supremacy",
			_ => string.Empty,
		};
	}

	public static string GetBattleRoom(int room)
	{
		return room switch
		{
			1 => "normal",
			2 => "training",
			4 or 5 => "tournament",
			7 => "rating",
			8 => "mad games",
			22 => "realistic",
			23 => "uprising",
			24 => "gravity force",
			25 => "skirmish",
			26 => "burning",
			27 => "boss fight",
			_ => string.Empty
		};
	}
}
