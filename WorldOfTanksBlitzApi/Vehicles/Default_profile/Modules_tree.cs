using JsonObjectConverter;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FMWOTB.Vehicles.Default_profile
{
	public class Modules_tree
	{
		public bool is_default
		{
			get; private set;
		}
		public int module_id
		{
			get; private set;
		}
		public string name
		{
			get; private set;
		}
		public List<int> next_modules
		{
			get; private set;
		}
		public List<int> next_tanks
		{
			get; private set;
		}
		public int price_credit
		{
			get; private set;
		}
		public int price_xp
		{
			get; private set;
		}
		public string type
		{
			get; private set;
		}

		public Modules_tree(Json json)
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
						var property = GetType().GetProperty(item1);
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
			if (helper.subJsons != null)
			{
				foreach (Json subJson in helper.subJsons)
				{
					setValues(subJson);
					if (subJson.tupleList == null)
					{
						string item1 = subJson.head.Trim(' ').Trim('\"');
						var property = GetType().GetProperty(item1);
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
			}
		}
	}
}
