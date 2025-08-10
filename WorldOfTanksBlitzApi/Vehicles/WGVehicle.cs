using WorldOfTanksBlitzApi.Exceptions;
using WorldOfTanksBlitzApi.Vehicles.Default_profile;
using JsonObjectConverter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WorldOfTanksBlitzApi.Vehicles
{
	public class WGVehicle
	{
		public string description
		{
			get; private set;
		}
		public List<int> engines
		{
			get; private set;
		}
		public List<int> guns
		{
			get; private set;
		}
		public bool is_premium
		{
			get; private set;
		}
		public string name
		{
			get; private set;
		}
		public string nation
		{
			get; private set;
		}
		public List<Tuple<string, string>> next_tanks
		{
			get; private set;
		}
		public List<Tuple<string, string>> prices_xp
		{
			get; private set;
		}
		public List<int> suspensions
		{
			get; private set;
		}
		public long tank_id
		{
			get; private set;
		}
		public int tier
		{
			get; private set;
		}
		public List<int> turrets
		{
			get; private set;
		}
		public string type
		{
			get; private set;
		}
		public Cost cost
		{
			get; private set;
		}
		public default_profile default_profile
		{
			get; private set;
		}
		public Images images
		{
			get; private set;
		}
		public Modules_tree modules_tree
		{
			get; private set;
		}

		/// <summary>
		/// Converts a Json object into a WGVehicle.
		/// The Json object must be starting from the the layer in Data!
		/// </summary>
		/// <param name="json"></param>
		public WGVehicle(Json json)
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
					try
					{
						switch (subJson.head.ToLower())
						{
							case "default_profile":
								this.default_profile = new default_profile(subJson);
								break;
							case "modules_tree":
								this.modules_tree = new Modules_tree(subJson);
								break;
							case "images":
								this.images = new Images(subJson);
								break;
							case "cost":
								this.cost = new Cost(subJson);
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
					setValues(subJson);
				}
			}
		}

		public static async Task<string> vehiclesToString(string key)
		{
			return await vehiclesToString(key, -1);
		}
		public static async Task<string> vehiclesToString(string key, long tank_id)
		{
			return await vehiclesToString(key, tank_id, null);
		}
		public static async Task<string> vehiclesToString(string key, List<string> fields)
		{
			return await vehiclesToString(key, -1, fields);
		}
		public static async Task<string> vehiclesToString(string key, List<long> tank_ids)
		{
			return await vehiclesToString(key, tank_ids, null);
		}
		public static async Task<string> vehiclesToString(string key, long tank_id, List<string> fields)
		{
			return await vehiclesToString(key, new List<long>() { tank_id }, fields);
		}
		/// <summary>
		/// Limit is 100 tankID's each time
		/// </summary>
		/// <param name="key"></param>
		/// <param name="tank_ids"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		public static async Task<string> vehiclesToString(string key, List<long> tank_ids, List<string> fields)
		{
			string url = @"https://api.wotblitz.eu/wotb/encyclopedia/vehicles/?application_id=" + key;
			HttpClient httpClient = new HttpClient();
			MultipartFormDataContent form1 = new MultipartFormDataContent();
			if (fields != null && fields.Count > 0)
			{
				StringBuilder sbFields = new StringBuilder();
				for (int i = 0; i < fields.Count; i++)
				{
					if (i > 0)
					{
						sbFields.Append("%2C");
					}
					sbFields.Append(fields[i]);
				}
				if (sbFields.Length > 0)
				{
					url += "&fields=" + sbFields.ToString();
				}
				//bool firstTime = true;
				//StringBuilder sb = new StringBuilder();
				//foreach (var field in fields)
				//{
				//    if (firstTime)
				//    {
				//        firstTime = false;
				//    }
				//    else
				//    {
				//        sb.Append(',');
				//    }
				//    sb.Append(field);
				//}
				//form1.Add(new StringContent(sb.ToString()), "fields");
			}
			StringBuilder sbTankIds = new StringBuilder();
			for (int i = 0; i < tank_ids.Count; i++)
			{
				if (tank_ids[i] > 0)
				{
					if (sbTankIds.Length > 0)
					{
						sbTankIds.Append("%2C");
					}
					sbTankIds.Append(tank_ids[i]);
				}
			}
			if (sbTankIds.Length > 0)
			{
				url += "&tank_id=" + sbTankIds.ToString();
			}
			HttpResponseMessage response = await httpClient.PostAsync(url, form1);
			if ((int) response.StatusCode >= 500)
			{
				throw new InternalServerErrorException();
			}
			return await response.Content.ReadAsStringAsync();
		}
	}
}
