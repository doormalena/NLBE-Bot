using JsonObjectConverter;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FMWOTB.Account
{
	public class Private
	{
		public string ban_info
		{
			get; private set;
		}
		public DateTime? ban_time
		{
			get; private set;
		}
		public int battle_life_time
		{
			get; private set;
		}
		public int bonds
		{
			get; private set;
		}
		public int credits
		{
			get; private set;
		}
		public int free_xp
		{
			get; private set;
		}
		public List<int> garage
		{
			get; private set;
		}
		public int gold
		{
			get; private set;
		}
		public bool is_bound_to_phone
		{
			get; private set;
		}
		public List<Tuple<long, string>> personal_missions
		{
			get; private set;
		}
		public DateTime? premium_expires_at
		{
			get; private set;
		}
		public Grouped_contacts grouped_contacts
		{
			get; private set;
		}
		public Private(Json json)
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
