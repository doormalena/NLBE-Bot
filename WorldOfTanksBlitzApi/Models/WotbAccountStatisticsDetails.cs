namespace WorldOfTanksBlitzApi.Models;

using System.Text.Json.Serialization;

public class WotbAccountStatisticsDetails
{
	[JsonInclude, JsonPropertyName("battles")]
	public int Battles
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("capture_points")]
	public int CapturePoints
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("damage_dealt")]
	public int DamageDealt
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("damage_received")]
	public int DamageReceived
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("dropped_capture_points")]
	public int DroppedCapturePoints
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("frags")]
	public int Frags
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("frags8p")]
	public int Frags8p
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("hits")]
	public int Hits
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("losses")]
	public int Losses
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("max_frags")]
	public int MaxFrags
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("max_frags_tank_id")]
	public long MaxFragsTankId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("max_xp")]
	public int MaxXp
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("max_xp_tank_id")]
	public long MaxXpTankId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("shots")]
	public int Shots
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("spotted")]
	public int Spotted
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("survived_battles")]
	public int SurvivedBattles
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("win_and_survived")]
	public int WinAndSurvived
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("wins")]
	public int Wins
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("xp")]
	public int Xp
	{
		get; internal set;
	}
}
