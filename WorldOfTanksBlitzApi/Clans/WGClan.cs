using FMWOTB.Exceptions;
using FMWOTB.Tools;
using JsonObjectConverter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FMWOTB.Clans
{
	public class WGClan
	{
		private static int MAX_RESULTS = 20;
		public int clan_id
		{
			get; private set;
		}
		public DateTime? joined_at { get; private set; } = null;
		public DateTime? created_at { get; private set; } = null;
		public DateTime? renamed_at { get; private set; } = null;
		public int emblem_set_id
		{
			get; private set;
		}
		public int members_count
		{
			get; private set;
		}
		public string recruiting_policy
		{
			get; private set;
		}
		public string motto
		{
			get; private set;
		}
		public string description
		{
			get; private set;
		}
		public string name
		{
			get; private set;
		}
		public string old_name
		{
			get; private set;
		}
		public string old_tag
		{
			get; private set;
		}
		public string creator_name
		{
			get; private set;
		}
		public string leader_name
		{
			get; private set;
		}
		public long leader_id
		{
			get; private set;
		}
		public long creator_id
		{
			get; private set;
		}
		public string tag
		{
			get; private set;
		}
		public string role
		{
			get; private set;
		}
		public bool is_clan_disbanded
		{
			get; private set;
		}
		public List<Members> members
		{
			get; private set;
		}
		public Recruiting_options recruiting_options
		{
			get; private set;
		}

		public WGClan(string wg_application_id, long clan_id, bool loadMembers)
		{
			Json clanDetails = new Json(clanDetailsInfoToString(wg_application_id, clan_id).Result, "WGClanDetails");
			if (clanDetails != null)
			{
				if (clanDetails.subJsons != null)
				{
					foreach (Json subJson in clanDetails.subJsons)
					{
						if (subJson.head.ToLower().Equals("data"))
						{
							setValues(clanDetails, loadMembers);
							break;
						}
					}
				}
			}
		}
		public WGClan(Json json, bool loadMembers)
		{
			setValues(json, loadMembers);
		}
		private void setValues(Json helper, bool loadMembers)
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
							case "recruiting_options":
								this.recruiting_options = new Recruiting_options(subJson);
								break;
							case "members":
								if (loadMembers)
								{
									if (subJson.subJsons != null)
									{
										if (subJson.subJsons.Count > 0)
										{
											this.members = new List<Members>();
											foreach (Json subSubJson in subJson.subJsons)
											{
												this.members.Add(new Members(subSubJson));
											}
										}
									}
								}
								break;
							default:
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
								break;
						}
					}
					catch (Exception)
					{
					}
					if (!subJson.head.ToLower().Equals("members"))
					{
						setValues(subJson, loadMembers);
					}
				}
			}
		}
		public void insertInfo(Json json, bool loadMembers)
		{
			setValues(json, loadMembers);
		}
		public static async Task<string> accountClanInfoToString(string key, long account_id)
		{
			string url = @"https://api.wotblitz.eu/wotb/clans/accountinfo/?application_id=" + key;
			//             https://api.wotblitz.eu/wotb/clans/accountinfo/?application_id=35e9c6a1ad0224a0a2950bb159522aaa
			HttpClient httpClient = new HttpClient();
			MultipartFormDataContent form1 = new MultipartFormDataContent();
			if (account_id > 0)
			{
				form1.Add(new StringContent(account_id.ToString()), "account_id");
				form1.Add(new StringContent("clan"), "extra");
			}
			HttpResponseMessage response = await httpClient.PostAsync(url, form1);
			if ((int) response.StatusCode >= 500)
			{
				throw new InternalServerErrorException();
			}
			return await response.Content.ReadAsStringAsync();
		}
		public static async Task<string> clanDetailsInfoToString(string key, long clan_id)
		{
			string url = @"https://api.wotblitz.eu/wotb/clans/info/?application_id=" + key;
			//             https://api.wotblitz.eu/wotb/clans/info/?application_id=35e9c6a1ad0224a0a2950bb159522aaa
			HttpClient httpClient = new HttpClient();
			MultipartFormDataContent form1 = new MultipartFormDataContent();
			if (clan_id > 0)
			{
				form1.Add(new StringContent(clan_id.ToString()), "clan_id");
				form1.Add(new StringContent("members"), "extra");
				//form1.Add(new StringContent("recruiting_options"), "extra");
			}
			HttpResponseMessage response = await httpClient.PostAsync(url, form1);
			if ((int) response.StatusCode >= 500)
			{
				throw new InternalServerErrorException();
			}
			return await response.Content.ReadAsStringAsync();
		}
		private static async Task<string> searchAccountByString(string key, string searchTerm)
		{
			string url = @"https://api.wotblitz.eu/wotb/clans/list/?application_id=" + key;
			HttpClient httpClient = new HttpClient();
			MultipartFormDataContent form1 = new MultipartFormDataContent();
			form1.Add(new StringContent(searchTerm), "search");
			HttpResponseMessage response = await httpClient.PostAsync(url, form1);
			if ((int) response.StatusCode >= 500)
			{
				throw new InternalServerErrorException();
			}
			return await response.Content.ReadAsStringAsync();
		}
		public static async Task<IReadOnlyList<WGClan>> searchByName(SearchAccuracy accuracy, string term, string wg_application_key, bool loadMembers)
		{
			string jsonText = await WGClan.searchAccountByString(wg_application_key, term);
			List<WGClan> clanList = new List<WGClan>();
			Json json = new Json(jsonText, "WGAccountList");
			if (json != null && json.jsonArray != null && json.jsonArray.Count > 0)
			{
				if (json.jsonArray.Count > WGClan.MAX_RESULTS)
				{
					throw new TooManyResultsException();
				}
				foreach (var subJsonItem in json.jsonArray)
				{
					var subJson = subJsonItem.Item2;
					if (subJsonItem.Item1.ToLower().Contains("data"))
					{
						if (subJson.tupleList.Count <= 5)
						{
							bool addToList = false;
							//Check nickname
							switch (accuracy)
							{
								case SearchAccuracy.EXACT:
									if (subJson.tupleList[3].Item2.Item1.Trim(' ').Trim('\"').Equals(term))
									{
										addToList = true;
									}
									break;
								case SearchAccuracy.STARTS_WITH:
									if (subJson.tupleList[3].Item2.Item1.Trim(' ').Trim('\"').StartsWith(term))
									{
										addToList = true;
									}
									break;
								case SearchAccuracy.EXACT_CASE_INSENSITIVE:
									if (subJson.tupleList[3].Item2.Item1.Trim(' ').Trim('\"').ToLower().Equals(term.ToLower()))
									{
										addToList = true;
									}
									break;
								case SearchAccuracy.STARTS_WITH_CASE_INSENSITIVE:
									if (subJson.tupleList[3].Item2.Item1.Trim(' ').Trim('\"').ToLower().StartsWith(term.ToLower()))
									{
										addToList = true;
									}
									break;
							}
							if (addToList)
							{
								clanList.Add(new WGClan(wg_application_key, Convert.ToInt64(subJson.tupleList[2].Item2.Item1.Trim(' ').Trim('\"')), loadMembers));
							}
						}
					}
				}
			}
			IReadOnlyList<WGClan> tempClanList = clanList;
			return tempClanList;
		}
		public static void changeMaxResults(int MAX_RESULTS)
		{
			WGClan.MAX_RESULTS = MAX_RESULTS;
		}
	}
}
