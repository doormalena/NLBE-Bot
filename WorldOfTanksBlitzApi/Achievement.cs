using WorldOfTanksBlitzApi.Exceptions;
using JsonObjectConverter;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WorldOfTanksBlitzApi
{
	public class Achievement
	{
		public string achievement_id
		{
			get; private set;
		}
		public string condition
		{
			get; private set;
		}
		public string description
		{
			get; private set;
		}
		public string image
		{
			get; private set;
		}
		public string image_big
		{
			get; private set;
		}
		public string name
		{
			get; private set;
		}
		public int order
		{
			get; private set;
		}
		public string section
		{
			get; private set;
		}
		public Option option
		{
			get; private set;
		}
		public static List<Achievement> achievements;
		public static Dictionary<int, string> achievementsFromWotinspector = new Dictionary<int, string> { { 601, "titleSniper" }, { 602, "invincible" }, { 603, "diehard" }, { 604, "handOfDeath" }, { 605, "armorPiercer" }, { 606, "cadet" }, { 607, "firstBlood" }, { 608, "firstVictory" }, { 609, "androidTest" }, { 610, "mechanicEngineer" }, { 611, "tankExpert" }, { 612, "mechanicEngineer0" }, { 613, "tankExpert0" }, { 614, "mechanicEngineer1" }, { 615, "tankExpert1" }, { 616, "mechanicEngineer2" }, { 617, "tankExpert2" }, { 618, "mechanicEngineer3" }, { 619, "tankExpert3" }, { 620, "mechanicEngineer4" }, { 621, "tankExpert4" }, { 622, "mechanicEngineer5" }, { 623, "tankExpert5" }, { 624, "mechanicEngineer6" }, { 625, "tankExpert6" }, { 626, "mechanicEngineer7" }, { 627, "tankExpert7" }, { 401, "battleHeroes" }, { 402, "fragsBeast" }, { 403, "sniperSeries" }, { 404, "maxSniperSeries" }, { 405, "invincibleSeries" }, { 406, "maxInvincibleSeries" }, { 407, "diehardSeries" }, { 408, "maxDiehardSeries" }, { 409, "killingSeries" }, { 410, "maxKillingSeries" }, { 411, "piercingSeries" }, { 412, "maxPiercingSeries" }, { 413, "warrior" }, { 414, "invader" }, { 415, "sniper" }, { 416, "defender" }, { 417, "steelwall" }, { 418, "supporter" }, { 419, "scout" }, { 420, "medalKay" }, { 421, "medalCarius" }, { 422, "medalKnispel" }, { 423, "medalPoppel" }, { 424, "medalAbrams" }, { 425, "medalLeClerc" }, { 426, "medalLavrinenko" }, { 427, "medalEkins" }, { 428, "medalWittmann" }, { 429, "medalOrlik" }, { 430, "medalOskin" }, { 431, "medalHalonen" }, { 432, "medalBurda" }, { 433, "medalBillotte" }, { 434, "medalKolobanov" }, { 435, "medalFadin" }, { 436, "raider" }, { 437, "kamikaze" }, { 438, "lumberjack" }, { 439, "beasthunter" }, { 440, "mousebane" }, { 441, "evileye" }, { 442, "medalRadleyWalters" }, { 443, "medalLafayettePool" }, { 444, "medalBrunoPietro" }, { 445, "medalTarczay" }, { 446, "medalPascucci" }, { 447, "medalDumitr" }, { 448, "markOfMastery" }, { 449, "medalLehvaslaiho" }, { 450, "medalNikolas" }, { 451, "fragsSinai" }, { 452, "sinai" }, { 453, "heroesOfRassenay" }, { 454, "medalBrothersInArms" }, { 455, "medalCrucialContribution" }, { 456, "medalDeLanglade" }, { 457, "medalTamadaYoshio" }, { 458, "bombardier" }, { 459, "huntsman" }, { 460, "alaric" }, { 461, "sturdy" }, { 462, "ironMan" }, { 463, "luckyDevil" }, { 464, "fragsPatton" }, { 465, "pattonValley" }, { 466, "memberBetaTest" }, { 467, "jointVictoryCount" }, { 468, "jointVictory" }, { 469, "punisherCount" }, { 470, "punisher" }, { 471, "medalSupremacy" }, { 472, "camper" }, { 473, "mainGun" }, { 474, "markOfMasteryI" }, { 475, "markOfMasteryII" }, { 476, "markOfMasteryIII" } };

		public Achievement(Json json)
		{
			setValues(json);
		}
		private void setValues(Json helper)
		{
			if (helper.tupleList != null)
			{
				foreach (var tuple in helper.tupleList)
				{
					string temp = tuple.Item2.Item1.Trim(' ').Trim('\"');
					if (!temp.ToLower().Equals("null"))
					{
						string item1 = tuple.Item1.Trim(' ').Trim('\"');
						var property = this.GetType().GetProperty(item1);
						if (property != null)
						{
							object valueToSet;
							if (item1.StartsWith("[") || item1.StartsWith("{"))
							{
								valueToSet = Json.convertStringToList(item1, property.PropertyType);
							}
							else
							{
								valueToSet = Json.convertStringToType(temp, property.PropertyType);
							}
							property.SetValue(this, valueToSet);
						}
					}
				}
			}
			if (helper.subJsons != null)
			{
				foreach (Json subJson in helper.subJsons)
				{
					if (subJson.head.ToLower().Equals("option"))
					{
						this.option = new Option(subJson);
					}
				}
			}
		}
		public static async Task<string> achievementsToString(string key, List<string> fields = null)
		{
			string url = @"https://api.wotblitz.eu/wotb/encyclopedia/achievements/?application_id=" + key;
			HttpClient httpClient = new HttpClient();
			MultipartFormDataContent form1 = new MultipartFormDataContent();
			if (fields != null && fields.Count > 0)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < fields.Count; i++)
				{
					if (i > 0)
					{
						sb.Append("%2C");
					}
					sb.Append(fields[i]);
				}
				url += "&fields=" + sb.ToString();
			}
			HttpResponseMessage response = await httpClient.PostAsync(url, form1);
			if ((int) response.StatusCode >= 500)
			{
				throw new InternalServerErrorException();
			}
			string tempString = await response.Content.ReadAsStringAsync();
			if (Achievement.achievements == null || Achievement.achievements.Count == 0)
			{
				setAchievements(new Json(tempString, string.Empty));
				achievements = achievements.OrderBy(x => x.achievement_id).ToList();
			}
			return tempString;
		}
		public static async Task<Achievement> getAchievement(string key, int id, List<string> fields = null)
		{
			await getAchievements(key, fields);
			List<string> tempNames = getAchievementNames();
			foreach (KeyValuePair<int, string> achievementFromWotinspector in achievementsFromWotinspector)
			{
				if (id.Equals(achievementFromWotinspector.Key))
				{
					foreach (Achievement achievement in achievements)
					{
						if (achievement.achievement_id.ToLower().Equals(achievementFromWotinspector.Value.ToLower()))
						{
							return achievement;
						}
					}
				}
			}
			return null;
		}
		public static async Task<List<Achievement>> getAchievements(string key, List<string> fields = null)
		{
			if (achievements != null && achievements.Count > 0)
			{
				return achievements;
			}
			else
			{
				await achievementsToString(key, fields);
				return achievements;
			}
		}

		private static void setAchievements(Json json)
		{
			setjsonAchievements(json, true);
		}
		private static void setjsonAchievements(Json json, bool reset)
		{
			if (achievements == null || reset)
			{
				achievements = new List<Achievement>();
			}
			if (json.head.Equals(string.Empty) && json.subJsons != null && json.subJsons.Count > 0)
			{
				foreach (Json subjson in json.subJsons)
				{
					if (subjson.head.ToLower().Equals("data") && subjson.subJsons != null)
					{
						foreach (Json subsubjson in subjson.subJsons)
						{
							achievements.Add(new Achievement(subsubjson));
						}
					}
				}
			}
			else if (json.head.ToLower().Equals("data") && json.subJsons != null)
			{
				foreach (Json subjson in json.subJsons)
				{
					achievements.Add(new Achievement(subjson));
				}
			}
			else if (json.tupleList != null && json.tupleList.Count == 9)
			{
				achievements.Add(new Achievement(json));
			}
			achievements = achievements.OrderBy(x => x.order).ToList();
		}

		private static List<string> getAchievementNames()
		{
			List<string> list = new List<string>();
			foreach (Achievement anAchievement in achievements)
			{
				list.Add(anAchievement.achievement_id);
			}
			return list;
		}
	}
}
