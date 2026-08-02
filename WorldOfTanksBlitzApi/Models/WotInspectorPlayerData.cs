namespace WorldOfTanksBlitzApi.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotInspectorPlayerData
{
	[JsonInclude, JsonPropertyName("team")]
	public int Team
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("name")]
	public string Name { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("entity_id")]
	public long EntityId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("dbid")]
	public long DbId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("clanid")]
	public long ClanId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("clan_tag")]
	public string ClanTag { get; internal set; } = string.Empty;
	[JsonInclude, JsonPropertyName("hitpoints_left")]
	public int HitpointsLeft
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("credits")]
	public int Credits
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp")]
	public int Exp
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("shots_made")]
	public int ShotsMade
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("shots_hit")]
	public int ShotsHit
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("shots_splash")]
	public int ShotsSplash
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("shots_pen")]
	public int ShotsPen
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("damage_made")]
	public int DamageMade
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("damage_received")]
	public int DamageReceived
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("damage_assisted")]
	public int DamageAssisted
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("damage_assisted_track")]
	public int DamageAssistedTrack
	{
		get; internal set;
	}

	public int DamageAssistedCombined => DamageAssisted + DamageAssistedTrack;

	[JsonInclude, JsonPropertyName("hits_received")]
	public int HitsReceived
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("hits_bounced")]
	public int HitsBounced
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("hits_splash")]
	public int HitsSplash
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("hits_pen")]
	public int HitsPen
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("enemies_spotted")]
	public int EnemiesSpotted
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("enemies_damaged")]
	public int EnemiesDamaged
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("enemies_destroyed")]
	public int EnemiesDestroyed
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("time_alive")]
	public int TimeAlive
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("distance_travelled")]
	public int DistanceTravelled
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("killed_by")]
	public long KilledBy
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("base_capture_points")]
	public int BaseCapturePoints
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("base_defend_points")]
	public int BaseDefendPoints
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp_for_damage")]
	public int ExpForDamage
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp_for_assist")]
	public int ExpForAssist
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("exp_team_bonus")]
	public int ExpTeamBonus
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("wp_points_earned")]
	public int WpPointsEarned
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("wp_points_stolen")]
	public int WpPointsStolen
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("hero_bonus_credits")]
	public int HeroBonusCredits
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("hero_bonus_exp")]
	public int HeroBonusExp
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("death_reason")]
	public int DeathReason
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("achievements")]
	public Dictionary<string, int> Achievements { get; internal set; } = [];

	[JsonInclude, JsonPropertyName("vehicle_descr")]
	public int VehicleDescr
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("turret_id")]
	public int TurretId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("gun_id")]
	public int GunId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("chassis_id")]
	public int ChassisId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("squad_index")]
	public int SquadIndex
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("damage_blocked")]
	public int DamageBlocked
	{
		get; internal set;
	}
}
