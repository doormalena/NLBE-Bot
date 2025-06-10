namespace NLBE_Bot.Blitzstars;

using Newtonsoft.Json;
using NLBE_Bot.Helpers;
using NLBE_Bot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Handler
{
	public static List<Tank> Filter90DaysStats(List<Tank> tankHistories)
	{
		var list = new List<Tank>();
		foreach (var tankHistory in tankHistories)
		{
			var dateTime = ConvertToDateTime(tankHistory.last_battle_time);
			var diff = DateTime.Now - dateTime;
			if (diff.TotalDays < 91)
			{
				list.Add(tankHistory);
			}
		}
		return list.Distinct().ToList();
	}
	public static List<Tank> Filter60DaysStats(List<Tank> tankHistories)
	{
		var list = new List<Tank>();
		foreach (var tankHistory in tankHistories)
		{
			var dateTime = ConvertToDateTime(tankHistory.last_battle_time);
			var diff = DateTime.Now - dateTime;
			if (diff.TotalDays < 61)
			{
				list.Add(tankHistory);
			}
		}
		return list.Distinct().ToList();
	}
	public static List<Tank> Filter30DaysStats(List<Tank> tankHistories)
	{
		var list = new List<Tank>();
		foreach (var tankHistory in tankHistories)
		{
			var dateTime = ConvertToDateTime(tankHistory.last_battle_time);
			var diff = DateTime.Now - dateTime;
			if (diff.TotalDays < 31)
			{
				list.Add(tankHistory);
			}
		}
		return list.Distinct().ToList();
	}
	public static List<TankHistory> Filter90DaysStats(List<TankHistory> tankHistories)
	{
		var list = new List<TankHistory>();
		foreach (var tankHistory in tankHistories)
		{
			var dateTime = ConvertToDateTime(tankHistory.last_battle_time);
			var diff = DateTime.Now - dateTime;
			if (diff.TotalDays < 91)
			{
				list.Add(tankHistory);
			}
		}
		return list.Distinct().ToList();
	}
	public static List<TankHistory> Filter60DaysStats(List<TankHistory> tankHistories)
	{
		var list = new List<TankHistory>();
		foreach (var tankHistory in tankHistories)
		{
			var dateTime = ConvertToDateTime(tankHistory.last_battle_time);
			var diff = DateTime.Now - dateTime;
			if (diff.TotalDays < 61)
			{
				list.Add(tankHistory);
			}
		}
		return list.Distinct().ToList();
	}
	public static List<TankHistory> Filter30DaysStats(List<TankHistory> tankHistories)
	{
		var list = new List<TankHistory>();
		foreach (var tankHistory in tankHistories)
		{
			var dateTime = ConvertToDateTime(tankHistory.last_battle_time);
			var diff = DateTime.Now - dateTime;
			if (diff.TotalDays < 31)
			{
				list.Add(tankHistory);
			}
		}
		return list.Distinct().ToList();
	}
	public static DateTime ConvertToDateTime(long tankHistoryLastBattleTime)
	{
		var aDatetime = new DateTime(1970, 1, 1).AddSeconds(tankHistoryLastBattleTime).ToUniversalTime();
		if (IsWinter(aDatetime))
			aDatetime = aDatetime.AddHours(1.0);
		return aDatetime;
	}
	private static bool IsWinter(DateTime aDatetime)
	{
		DateTime dateTime1 = new DateTime(aDatetime.Year, 10, 1);
		List<DateTime> dateTimeList1 = new List<DateTime>();
		for (int index = 0; index < 31; ++index)
		{
			if (dateTime1.DayOfWeek == DayOfWeek.Sunday)
				dateTimeList1.Add(dateTime1);
			dateTime1 = dateTime1.AddDays(1.0);
		}
		dateTime1 = dateTimeList1[dateTimeList1.Count - 1];
		DateTime dateTime2 = new DateTime(aDatetime.Year, 3, 1);
		List<DateTime> dateTimeList2 = new List<DateTime>();
		for (int index = 0; index < 31; ++index)
		{
			if (dateTime2.DayOfWeek == DayOfWeek.Sunday)
				dateTimeList2.Add(dateTime2);
			dateTime2 = dateTime2.AddDays(1.0);
		}
		dateTime2 = dateTimeList2[dateTimeList2.Count - 1];
		return aDatetime >= dateTime1 || dateTime2 < aDatetime;
	}

	public static Dictionary<int, PlayerTankAndTankHistory> Combine(List<Vehicle> vehicleOfPlayers, List<TankHistory> tankHistoryList)
	{
		var ptathList = new Dictionary<int, PlayerTankAndTankHistory>();
		foreach (var tankHistory in tankHistoryList)
		{
			if (!ptathList.Keys.Contains(tankHistory.tank_id))
			{
				var single = vehicleOfPlayers.Where(vop => vop.tank_id == tankHistory.tank_id);
				var x = tankHistoryList.Where(thl => thl.tank_id == tankHistory.tank_id);
				var ptath = new PlayerTankAndTankHistory()
				{
					PlayerTank = single.Count() > 0 ? single.ElementAt(0) : new Vehicle() { tank_id = x.ElementAt(0).tank_id, all = new All() },
					TnkHistoryList = x.ToList()
				};
				ptathList.Add(tankHistory.tank_id, ptath);
			}
		}
		return H(ptathList);
	}

	private static Dictionary<int, PlayerTankAndTankHistory> H(Dictionary<int, PlayerTankAndTankHistory> dict)
	{
		const long a = 1525392024000;
		var t = DateTimeNow() / 1E3 - 864E4;
		if (a / 1E3 > t - 864E3)
		{
			return dict;
		}
		foreach (var i in dict.Values)
		{
			if (i.TnkHistoryList[0].last_battle_time > t + 864E3)
			{
				i.TnkHistoryList.Add(new TankHistory()
				{
					account_id = i.TnkHistoryList[0].account_id,
					last_battle_time = (long) Math.Round(i.TnkHistoryList[0].last_battle_time - 864E3),
					region = i.TnkHistoryList[0].region,
					tank_id = i.TnkHistoryList[0].tank_id,
					all = new All()
				});
				i.TnkHistoryList = i.TnkHistoryList.OrderBy(x => x.last_battle_time).ThenByDescending(x => x._id).ToList();
			}
		}
		return dict;
	}

	private static TankHistory N(int e, TankHistory[] a)
	{
		var t = DateTimeNow() / 1E3;
		var i = t - 24 * e * 60 * 60;
		var accumulated = t;
		TankHistory returnTankHistory = null;
		for (var index = 0; index < a.Length; index++)
		{
			if (a[index].last_battle_time > i && a[index].last_battle_time < accumulated)
			{
				if (index > 0 && a[index - 1] != null)
				{
					var e2 = a[index].last_battle_time - 86400;
					var l = a[index].last_battle_time - 604800;
					var n = 30 == a[index].update_type || 30 == a[index - 1].update_type;
					if (e2 > i && n || l > i)
					{
						if (returnTankHistory == null || returnTankHistory.tank_id == 0)
						{
							returnTankHistory = a[index - 1];
						}
						continue;
					}
				}
				if (returnTankHistory == null || returnTankHistory.tank_id == 0)
				{
					returnTankHistory = a[index];
				}
				continue;
			}
			if (returnTankHistory == null)
			{
				returnTankHistory = new TankHistory()
				{
					last_battle_time = (long) t
				};
			}
		}
		return returnTankHistory;
	}
	public static int Get90DayBattles(long accountId)
	{
		var response = ApiRequester.GetRequest("https://www.blitzstars.com/api/tankhistories/for/" + accountId);
		var tankHistories = JsonConvert.DeserializeObject<List<TankHistory>>(response);
		var responseVehicles = ApiRequester.GetRequest("https://api.wotblitz.eu/wotb/tanks/stats/?application_id=" + Bot.WarGamingAppId + "&account_id=" + accountId);
		responseVehicles = Regex.Replace(responseVehicles, "\"data\":{\"([0-9]*)\"", "\"data\":{\"Vehicles\"", RegexOptions.NonBacktracking);
		var playerVehicleData = JsonConvert.DeserializeObject<PlayerVehicle>(responseVehicles);
		var combined = Handler.Combine(playerVehicleData.data.Vehicles.ToList(), tankHistories);
		var dict = new Dictionary<int, TankHistory[]>();
		const int amountOfDaysToFilerOn = 90;
		var totalBattles90 = 0;

		while (combined.Count > 0)
		{
			var firstValue = combined.First().Value;
			var fullArray = new List<TankHistory>();
			fullArray.Add(new TankHistory(firstValue.PlayerTank));
			for (var i = 0; i < firstValue.TnkHistoryList.Count; i++)
			{
				fullArray.Add(firstValue.TnkHistoryList[i]);
			}
			// fullArray.Sort((e,a)=>e.last_battle_time > a.last_battle_time ? 1 : -1);
			fullArray = fullArray.OrderBy(x => x.last_battle_time).ThenByDescending(x => x._id).ToList();
			dict.Add(fullArray[0].tank_id, fullArray.ToArray());
			combined.Remove(firstValue.PlayerTank.tank_id);
		}

		var v = Handler.V(dict);
		v = v.OrderBy(x => x.Key).ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);
		var battles30d = 0;
		var battles60d = 0;
		var battles90d = 0;
		var tankInfoList = new List<TankInfo>();
		var tankInfoList30 = new Dictionary<int, TankInfo>();
		var tankInfoList60 = new Dictionary<int, TankInfo>();
		var tankInfoList90 = new Dictionary<int, TankInfo>();
		foreach (var tankInfo in v.Values)
		{
			bool added = false;
			if (tankInfo.Period30d != null)
			{
				battles30d += tankInfo.Period30d.all.battles;
				tankInfoList.Add(tankInfo);
				tankInfoList30.Add(tankInfo.Live.tank_id, tankInfo);
				added = true;
			}
			if (tankInfo.Period60d != null)
			{
				battles60d += tankInfo.Period60d.all.battles;
				tankInfoList60.Add(tankInfo.Live.tank_id, tankInfo);
				if (!added)
				{
					tankInfoList.Add(tankInfo);
					added = true;
				}
			}
			if (tankInfo.Period90d != null)
			{
				battles90d += tankInfo.Period90d.all.battles;
				tankInfoList90.Add(tankInfo.Live.tank_id, tankInfo);
				if (!added)
				{
					tankInfoList.Add(tankInfo);
				}
			}
		}
		return battles90d;
	}
	public static Dictionary<int, TankInfo> V(Dictionary<int, TankHistory[]> e)
	{
		var t = new Dictionary<int, TankInfo>();
		foreach (var s in e)
		{
			t.Add(s.Key, R(s.Value));
		}
		return t;
	}
	private static TankInfo R(TankHistory[] e)
	{
		return new TankInfo()
		{
			Live = e[e.Length - 1],
			Period30d = D(e[e.Length - 1], N(30, e)),
			Period60d = D(e[e.Length - 1], N(60, e)),
			Period90d = D(e[e.Length - 1], N(90, e)),
			History = e
		};
	}
	private static TankHistory D(TankHistory e, TankHistory a)
	{
		if (e == null || e.tank_id == 0 || a == null || a.tank_id == 0)
			return null;
		var l = new TankHistory()
		{
			last_battle_time = e.last_battle_time,
			battle_life_time = e.battle_life_time - a.battle_life_time,
			tier = e.tier,
			tank_id = e.tank_id,
			all = new All(),
			start_record = a,
			end_record = e,
			start_date = a.last_battle_time,
			end_date = e.last_battle_time
		};
		l.all.spotted = e.all.spotted - a.all.spotted;
		l.all.hits = e.all.hits - a.all.hits;
		l.all.wins = e.all.wins - a.all.wins;
		l.all.win_and_survived = e.all.win_and_survived - a.all.win_and_survived;
		l.all.losses = e.all.losses - a.all.losses;
		l.all.capture_points = e.all.capture_points - a.all.capture_points;
		l.all.battles = e.all.battles - a.all.battles;
		l.all.damage_dealt = e.all.damage_dealt - a.all.damage_dealt;
		l.all.damage_received = e.all.damage_received - a.all.damage_received;
		l.all.shots = e.all.shots - a.all.shots;
		l.all.xp = e.all.xp - a.all.xp;
		l.all.survived_battles = e.all.survived_battles - a.all.survived_battles;
		l.all.frags = e.all.frags - a.all.frags;
		l.all.frags8p = e.all.frags8p - a.all.frags8p;
		l.all.dropped_capture_points = e.all.dropped_capture_points - a.all.dropped_capture_points;
		return l;
	}
	private static long DateTimeNow()
	{
		// return DateTimeOffset.Now.ToUnixTimeMilliseconds();
		var datum = new DateTime(1970, 1, 1);
		return (long) DateTime.Now.Subtract(datum).TotalMilliseconds;
	}
}
