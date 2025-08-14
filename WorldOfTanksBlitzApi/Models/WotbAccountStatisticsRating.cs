namespace WorldOfTanksBlitzApi.Models;

using System;
using System.Text.Json.Serialization;

public class WotbAccountStatisticsRating : WotbAccountStatisticsDetails
{
	[JsonInclude, JsonPropertyName("calibration_battles_left")]
	public int Calibration_battles_left
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("current_season")]
	public int Current_season
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("is_recalibration")]
	public bool IsRecalibration
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("mm_rating")]
	public float MMRating
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("recalibration_start_time")]
	public DateTime? RecalibrationStartTime
	{
		get; internal set;
	}
}
