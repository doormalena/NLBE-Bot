namespace FMWOTB.Account;

using FMWOTB.Exceptions;
using JsonObjectConverter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

internal class WGAccount // TODO: phase out this class and use the new FMWOTB.WotbAccountInfo instead
{
	public long account_id
	{
		get; private set;
	}
	public long clan_id
	{
		get; private set;
	}
	public DateTime? created_at
	{
		get; private set;
	}
	public DateTime? last_battle_time
	{
		get; private set;
	}
	public string nickname
	{
		get; private set;
	}
	public DateTime? updated_at
	{
		get; private set;
	}
	public Statistics.Statistics statistics
	{
		get; private set;
	}
	public Clans.WGClan clan
	{
		get; private set;
	}
	public List<Vehicles.VehicleOfPlayer> VehiclesOfPlayers
	{
		get; private set;
	}
	public string blitzstars
	{
		get
		{
			if (this.account_id > 0)
			{
				return "https://www.blitzstars.com/sigs/" + this.account_id;
			}
			else
			{
				return null;
			}
		}
	}
	/// <summary>
	/// Converts a Json object into a WGAccount.
	/// The Json object must be starting from the the layer in Data!
	/// loadVehicles:
	/// 0 = false
	/// 1 = true
	/// 2 = in garage only
	/// </summary>
	/// <param name="json"></param>
	public WGAccount(string application_id, long account_id, bool loadClanMembers = false, bool loadClan = false, bool loadStatistics = false, short loadVehicles = 0)
	{
		Json json = new Json(accountToString(application_id, account_id, string.Empty).Result, "WGAccount");
		if (json != null)
		{
			setValues(json, loadClan, loadStatistics, loadVehicles);
			this.setClanAsObject(application_id, account_id, loadClanMembers).Wait();
			if (loadVehicles == 1)
			{
				this.InitializeVehiclesOfPlayer(application_id, account_id).Wait();
			}
			else if (loadVehicles == 2)
			{
				this.InitializeGarageVehiclesOfPlayer(application_id, account_id).Wait();
			}

			if (json.subJsons != null)
			{
				foreach (Json subJson in json.subJsons)
				{
					if (subJson.head.ToLower().Equals("data"))
					{
						if (subJson.subJsons != null)
						{
							foreach (Json subSubJson in subJson.subJsons)
							{
								setValues(subSubJson, loadClan, loadStatistics, loadVehicles);
							}
						}
						break;
					}
				}
			}
		}
	}
	private void setValues(Json helper, bool loadClan, bool loadStatistics, short loadVehicles)
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
						else if (property.PropertyType == typeof(DateTime?))
						{
							if (property.GetValue(this) == null)
							{
								property.SetValue(this, new DateTime());
							}
							valueToSet = Json.convertStringToDateTime(temp);
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
				try
				{
					switch (subJson.head.ToLower())
					{
						case "statistics":
							if (loadStatistics)
							{
								this.statistics = new Statistics.Statistics(subJson);
							}
							break;
						case "vehiclesofplayers":
							break;
						case "clan":
							break;
						default:
							if (loadClan)
							{
								if (subJson.tupleList == null)
								{
									var property = this.GetType().GetProperty(subJson.head);
									if (property != null)
									{
										string item1 = subJson.jsonText.Trim(' ').Trim('\"');
										var valueToSet = Json.convertStringToList(item1, property.PropertyType);
										object instance = Activator.CreateInstance(property.PropertyType);
										// List<T> implements the non-generic IList interface
										IList list = (IList) instance;
										foreach (object item in valueToSet)
										{
											list.Add(item);
										}
										property.SetValue(this, list, null);
									}
								}
								else
								{
									var property = this.GetType().GetProperty(subJson.head.Trim(' ').Trim('\"'));
									if (property != null)
									{
										List<Tuple<string, string>> tupleList = new List<Tuple<string, string>>();
										foreach (var item in subJson.tupleList)
										{
											tupleList.Add(new Tuple<string, string>(item.Item1.Trim(' ').Trim('\"'), item.Item2.Item1.Trim(' ').Trim('\"')));
										}
										object instance = Activator.CreateInstance(property.PropertyType);
										// List<T> implements the non-generic IList interface
										IList list = (IList) instance;
										foreach (object item in tupleList)
										{
											list.Add(item);
										}
										property.SetValue(this, list, null);
									}
								}
							}
							break;
					}
				}
				catch (Exception)
				{
				}
				setValues(subJson, loadClan, loadStatistics, loadVehicles);
			}
		}
	}
	private async Task InitializeVehiclesOfPlayer(string key, long account_id)
	{
		Json playerVehiclesJson = new Json(await AllVehiclesOfPlayerToString(key, account_id), "VehiclesOfPlayer");
		this.SetVehiclesOfPlayerByJson(playerVehiclesJson);
	}
	private async Task InitializeGarageVehiclesOfPlayer(string key, long account_id)
	{
		Json playerVehiclesJson = new Json(await GarageVehiclesOfPlayerToString(key, account_id), "VehiclesOfPlayer");
		this.SetVehiclesOfPlayerByJson(playerVehiclesJson);
	}
	private void SetVehiclesOfPlayerByJson(Json json)
	{
		if (json != null)
		{
			if (json.subJsons != null)
			{
				foreach (Json subJson in json.subJsons)
				{
					if (subJson.head.ToLower().Equals("data"))
					{
						if (subJson.jsonArray != null && subJson.jsonArray.Count > 0)
						{
							//hier GEEN parallel for zetten --> zorgt voor inconsistente data & ook inconsistente duur
							this.VehiclesOfPlayers = new List<Vehicles.VehicleOfPlayer>();
							foreach (var item in subJson.jsonArray)
							{
								this.VehiclesOfPlayers.Add(new Vehicles.VehicleOfPlayer(item.Item2));
							}
						}
						break;
					}
				}
			}
		}
	}
	public static async Task<string> accountToString(string key, long account_id, string searchTerm)
	{
		string url = @"https://api.wotblitz.eu/wotb/account/info/?application_id=" + key;
		HttpClient httpClient = new HttpClient();
		MultipartFormDataContent form1 = new MultipartFormDataContent();
		if (account_id > 0)
		{
			form1.Add(new StringContent(account_id.ToString()), "account_id");
			form1.Add(new StringContent("statistics.rating"), "extra");
		}
		else if (searchTerm.Length > 0)
		{
			form1.Add(new StringContent(searchTerm), "fields");
		}
		HttpResponseMessage response = await httpClient.PostAsync(url, form1);
		if ((int) response.StatusCode >= 500)
		{
			throw new InternalServerErrorException();
		}
		return await response.Content.ReadAsStringAsync();
	}

	private void setClanByJson(Json json, bool loadMembers)
	{
		if (json != null)
		{
			if (json.subJsons != null)
			{
				foreach (Json subJson in json.subJsons)
				{
					if (subJson.head.ToLower().Equals("data"))
					{
						if (subJson != null)
						{
							if (subJson.subJsons != null)
							{
								if (subJson.subJsons.Count > 0)
								{
									if (this.clan != null)
									{
										this.clan.insertInfo(subJson.subJsons[0], loadMembers);
									}
									else
									{
										this.clan = new Clans.WGClan(subJson.subJsons[0], loadMembers);
									}
								}
							}
						}
						break;
					}
				}
			}
		}
	}
	private async Task setClanAsObject(string key, long account_id, bool loadMembers)
	{
		Json jsonAccountClanInfo = new Json(await Clans.WGClan.accountClanInfoToString(key, account_id), "AccountClanInfo");
		this.setClanByJson(jsonAccountClanInfo, loadMembers);
		if (this.clan != null)
		{
			if (this.clan.clan_id > 0)
			{
				this.clan_id = this.clan.clan_id;
				Json jsonClanDetails = new Json(await Clans.WGClan.clanDetailsInfoToString(key, this.clan_id), "ClanDetails");
				this.setClanByJson(jsonClanDetails, loadMembers);
			}
		}
	}

	public static async Task<string> AllVehiclesOfPlayerToString(string key, long account_id)
	{
		string url = @"https://api.wotblitz.eu/wotb/tanks/stats/?application_id=" + key;
		//            https://api.wotblitz.eu/wotb/tanks/stats/?application_id=35e9c6a1ad0224a0a2950bb159522aaa&account_id=538258181
		HttpClient httpClient = new HttpClient();
		MultipartFormDataContent form1 = new MultipartFormDataContent();
		if (account_id > 0)
		{
			form1.Add(new StringContent(account_id.ToString()), "account_id");
		}
		HttpResponseMessage response = await httpClient.PostAsync(url, form1);
		if ((int) response.StatusCode >= 500)
		{
			throw new InternalServerErrorException();
		}
		return await response.Content.ReadAsStringAsync();
	}
	public static async Task<string> GarageVehiclesOfPlayerToString(string key, long account_id)
	{
		string url = @"https://api.wotblitz.eu/wotb/tanks/stats/?application_id=" + key + "&account_id=" + account_id + "&in_garage=1";
		//            https://api.wotblitz.eu/wotb/tanks/stats/?application_id=35e9c6a1ad0224a0a2950bb159522aaa&account_id=538258181
		HttpClient httpClient = new HttpClient();
		MultipartFormDataContent form1 = new MultipartFormDataContent();
		HttpResponseMessage response = await httpClient.PostAsync(url, form1);
		if ((int) response.StatusCode >= 500)
		{
			throw new InternalServerErrorException();
		}
		return await response.Content.ReadAsStringAsync();
	}
}
