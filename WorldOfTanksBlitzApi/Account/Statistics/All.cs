using JsonObjectConverter;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FMWOTB.Account.Statistics
{
	public class All
	{
		public int battles
		{
			get; private set;
		}
		public int capture_points
		{
			get; private set;
		}
		public int damage_dealt
		{
			get; private set;
		}
		public int damage_received
		{
			get; private set;
		}
		public int dropped_capture_points
		{
			get; private set;
		}
		public int frags
		{
			get; private set;
		}
		public int frags8p
		{
			get; private set;
		}
		public int hits
		{
			get; private set;
		}
		public int losses
		{
			get; private set;
		}
		public int max_frags
		{
			get; private set;
		}
		public long max_frags_tank_id
		{
			get; private set;
		}
		public int max_xp
		{
			get; private set;
		}
		public long max_xp_tank_id
		{
			get; private set;
		}
		public int shots
		{
			get; private set;
		}
		public int spotted
		{
			get; private set;
		}
		public int survived_battles
		{
			get; private set;
		}
		public int win_and_survived
		{
			get; private set;
		}
		public int wins
		{
			get; private set;
		}
		public int xp
		{
			get; private set;
		}
		public All(Json json)
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
							//case "default_profile": this.default_profile = new default_profile(subJson); break;
							//case "modules_tree": this.modules_tree = new Modules_tree(subJson); break;
							//case "images": this.images = new Images(subJson); break;
							//case "cost": this.cost = new Cost(subJson); break;
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
	}
}
