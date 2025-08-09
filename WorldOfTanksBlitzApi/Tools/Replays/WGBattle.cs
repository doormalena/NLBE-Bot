using FMWOTB.Exceptions;
using JsonObjectConverter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FMWOTB.Tools.Replays
{
	public class WGBattle /* : Battle*/
	{
		public string view_url
		{
			get; private set;
		}
		public string download_url
		{
			get; private set;
		}
		public int winner_team
		{
			get; private set;
		}
		public long uploaded_by
		{
			get; private set;
		}
		public int credits_total
		{
			get; private set;
		}
		public int exp_base
		{
			get; private set;
		}
		public string player_name
		{
			get; private set;
		}
		public string title
		{
			get; private set;
		}
		public Details details
		{
			get; private set;
		}
		public List<Details> fullDetails
		{
			get; private set;
		}
		public string vehicle
		{
			get; private set;
		}
		public List<long> enemies
		{
			get; private set;
		}
		public List<long> allies
		{
			get; private set;
		}
		public string description
		{
			get; private set;
		}
		public double battle_duration
		{
			get; private set;
		}
		public ulong arena_unique_id
		{
			get; private set;
		}
		public int vehicle_tier
		{
			get; private set;
		}
		public DateTime? battle_start_time
		{
			get; private set;
		}
		public int mastery_badge
		{
			get; private set;
		}
		public long protagonist
		{
			get; private set;
		}
		public int battle_type
		{
			get; private set;
		}
		public int exp_total
		{
			get; private set;
		}
		public int vehicle_type
		{
			get; private set;
		}
		public double battle_start_timestamp
		{
			get; private set;
		}
		public int credits_base
		{
			get; private set;
		}
		public int protagonist_team
		{
			get; private set;
		}
		public string map_name
		{
			get; private set;
		}
		public int room_type
		{
			get; private set;
		}
		public int battle_result
		{
			get; private set;
		}
		public string error
		{
			get; private set;
		}
		public string hexKey
		{
			get; private set;
		}
		public string view_online
		{
			get
			{
				if (this.hexKey != null && this.hexKey.Length > 0)
				{
					return "https://map.wotinspector.com/en/?url=https://replays.wotinspector.com/en/download/" + this.hexKey + "&frame&package=blitz8.2&platform=blitz";
				}
				else
				{
					return null;
				}
			}
		}
		public static StringBuilder log;

		/// <summary>
		/// Instantiate this object with the the replay url. This url is from a downloadable place such as discord or wotinspector.com
		/// </summary>
		public WGBattle(string replayUrlOrJson)
		{
			log = new StringBuilder();
			if (replayUrlOrJson != null && replayUrlOrJson.Length > 0)
			{
				if (replayUrlOrJson.StartsWith("http"))
				{
					getBattle(replayUrlOrJson, true).Wait();
				}
				else
				{
					getBattle(replayUrlOrJson, false).Wait();
					//jsonToBattle(replayUrlOrJson);
				}
				if (this.details == null)
				{
					if (this.fullDetails != null)
					{
						foreach (Details detail in this.fullDetails)
						{
							if (detail.dbid.Equals(this.protagonist))
							{
								this.details = detail;
							}
						}
					}
				}
			}
		}
		private async Task getBattle(string pathOrJson, bool isHTTP)
		{
			string jsonText = pathOrJson;
			if (isHTTP)
			{
				try
				{
					jsonText = await replayToString(pathOrJson, null);
				}
				catch
				{
					throw new Exception("Could not set jsonText from pathOrJson", new JsonConvertingException(pathOrJson));
				}
			}
			if (jsonText == null)
			{
				throw new JsonNotFoundException("jsonText == null");
			}
			else if (jsonText.Length == 0)
			{
				throw new JsonNotFoundException("jsonText.Length == 0");
			}
			try
			{
				jsonToBattle(jsonText);
			}
			catch
			{
				throw new Exception("Something went wrong in jsonToBattle(jsonText)", new JsonConvertingException(jsonText));
			}
		}
		private async Task<string> replayToString(string path, string title, ulong? wg_id = null)
		{
			string url = @"https://wotinspector.com/api/replay/upload?url=";
			HttpClient httpClient = new HttpClient();
			MultipartFormDataContent form1 = new MultipartFormDataContent();
			String AsBase64String = null;
			if (path.StartsWith("http"))
			{
				if (path.Contains("wotinspector"))
				{
					//return een json in deze else
					HttpResponseMessage iets =
						await httpClient.GetAsync("https://api.wotinspector.com/replay/upload?details=full&key="
						+ Path.GetFileName(path));
					if (iets != null)
					{
						if (iets.Content != null)
						{
							return await iets.Content.ReadAsStringAsync();
						}
					}
					return null;
				}
				else
				{
					AsBase64String = Convert.ToBase64String(await httpClient.GetByteArrayAsync(path));
				}
			}
			else if (path.Contains('\\') || path.Contains('/'))
			{
				AsBase64String = Convert.ToBase64String(File.ReadAllBytes(path));
			}

			form1.Add(new StringContent(Path.GetFileName(path)), "filename"); //filename
			form1.Add(new StringContent(AsBase64String), "file");
			if (title != null)
			{
				if (!title.Equals(string.Empty))
				{
					form1.Add(new StringContent(title), "title"); //title
				}
			}
			if (wg_id != null)
			{
				form1.Add(new StringContent(wg_id.Value.ToString()), "loaded_by");
			}

			HttpResponseMessage response = await httpClient.PostAsync(url, form1);
			if ((int) response.StatusCode >= 500)
			{
				throw new InternalServerErrorException();
			}
			return await response.Content.ReadAsStringAsync();





			//if (title == null)
			//{
			//    title = Path.GetFileName(path);
			//}
			//string url = @"https://wotinspector.com/api/replay/upload?url=";
			//HttpClient httpClient = new HttpClient();
			//MultipartFormDataContent form1 = new MultipartFormDataContent();
			//String AsBase64String = null;
			//if (path.StartsWith("http"))
			//{
			//    AsBase64String = Convert.ToBase64String(await httpClient.GetByteArrayAsync(path));
			//}
			//else
			//{
			//    AsBase64String = Convert.ToBase64String(File.ReadAllBytes(path));
			//}

			//form1.Add(new StringContent(Path.GetFileName(path)), "filename");
			//form1.Add(new StringContent(AsBase64String), "file");

			//HttpResponseMessage response = await httpClient.PostAsync(url, form1);
			//return await response.Content.ReadAsStringAsync();
		}
		private void jsonToBattle(string jsonText)
		{
			try
			{
				Json jsonHelper = new Json(jsonText, "WGBattle");
				if (jsonHelper.subJsons != null)
				{
					foreach (Json helper in jsonHelper.subJsons)
					{
						//WGBattle
						setValues(helper);
						if (helper.subJsons != null)
						{
							foreach (Json subHelper in helper.subJsons)
							{
								setValues(subHelper);
								if (subHelper.subJsons != null)
								{
									int detailsCounter = 0;
									foreach (Json subSubHelper in subHelper.subJsons)
									{
										if (subSubHelper.head.ToLower().Equals("details"))
										{
											detailsCounter++;
										}
									}
									if (detailsCounter > 1)
									{
										this.fullDetails = new List<Details>();
									}
									foreach (Json subSubHelper in subHelper.subJsons)
									{
										//details
										//enemies
										//alies

										if (subSubHelper.head.ToLower().Equals("details"))
										{
											if (detailsCounter > 1)
											{
												this.fullDetails.Add(new Details(subSubHelper));
											}
											else
											{
												this.details = new Details(subSubHelper);
											}
										}
										else
										{
											string[] splitted = subSubHelper.jsonText.Trim(' ').TrimStart('[').TrimEnd(']').Split(',');
											if (subSubHelper.head.ToLower().Equals("enemies"))
											{
												if (this.enemies == null)
												{
													this.enemies = new List<long>();
												}
												this.enemies.Add(Convert.ToInt64(splitted[0]));
											}
											else
											{
												if (this.allies == null)
												{
													this.allies = new List<long>();
												}
												this.allies.Add(Convert.ToInt64(splitted[0]));
											}
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("jonToBattle > exception gethrowd:\nMessage: " + ex.Message, new JsonConvertingException("jsonText:\n" + jsonText));
			}
		}
		private void setValues(Json helper)
		{
			try
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
								bool isValidValue = true;
								var valueToSet = new object();
								if (property.PropertyType == typeof(DateTime?))
								{
									valueToSet = Json.convertStringToDateTime(temp).AddHours(1); //Hardcoded er 1 uur bijgeteld omdat de json, van de site een uur vroeger dan het uur dat op de site staat, teruggeeft
																								 //valueToSet = Json.convertStringToDateTime(temp);
								}
								else
								{
									try
									{
										if (property.PropertyType == typeof(double))
										{
											double result = double.Parse(temp);
											valueToSet = result;

											//Try parsing in the current culture
											//if (!double.TryParse(temp.ToString(), System.Globalization.NumberStyles.Any, CultureInfo.CurrentCulture, out result))
											//{//Then try in US english
											//    if (!double.TryParse(temp.ToString(), System.Globalization.NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result))
											//    {
											//        //Then in neutral language
											//        if (!double.TryParse(temp.ToString(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out result))
											//        {
											//            isValidValue = false;
											//            Console.WriteLine("\nDOUBLE KON NIET GECONVERTEERD WORDEN!\nType: " + property.PropertyType.Name + "\ntemp: " + temp);
											//        }
											//    }
											//}

										}
										else
										{
											valueToSet = Convert.ChangeType(temp, property.PropertyType);
										}
									}
									catch (Exception)
									{
										isValidValue = false;
										//Console.WriteLine("\n\nDIT HEB IK NODIG!\n\nException bij Convert.ChangeType(temp, property.PropertyType)\n" + "Property: " + property.Name + "\ntemp: " + temp + "\nTuple dat geconvert moest worden:\nitem1 = " + tuple.Item1 + "\nitem2 = " + tuple.Item2 + "\nExceptionMessage: " + ex.Message + "\nStacktrace:\n" + ex.StackTrace + "\n\n");
									}
								}
								if (isValidValue)
								{
									property.SetValue(this, valueToSet);
									if (property.Name.ToLower().Contains("url") && this.hexKey == null)
									{
										this.hexKey = Path.GetFileName((string) valueToSet);
									}
								}
							}
						}
					}
				}
			}
			catch (Exception)
			{
				//Console.WriteLine("\n\nDIT HEB IK NODIG! (IS WEL NOG REDELIJK ALGEMEEN)\n\nExceptionMessage: " + ex.Message + "\nStacktrace:\n" + ex.StackTrace);
			}
		}
		public static string getBattleType(int type)
		{
			switch (type)
			{
				case 0:
					return "encounter";
				case 1:
					return "supremacy";
			}
			return string.Empty;
		}
		public static string getBattleRoom(int room)
		{
			switch (room)
			{
				case 1:
					return "normal";
				case 2:
					return "training";
				case 4:
					return "tournament";
				case 5:
					return "tournament";
				case 7:
					return "rating";
				case 8:
					return "mad games";
				case 22:
					return "realistic";
				case 23:
					return "uprising";
				case 24:
					return "gravity force";
				case 25:
					return "skirmish";
				case 26:
					return "burning";
				case 27:
					return "boss fight";
			}
			return string.Empty;
		}
	}
}
