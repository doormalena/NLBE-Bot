using JsonObjectConverter;
using System;
using System.Collections;

namespace WorldOfTanksBlitzApi.Vehicles
{
	public class Armor
	{
		public Hull hull
		{
			get; private set;
		}
		public Turret turret
		{
			get; private set;
		}

		public Armor(Json json)
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
						if (item1.StartsWith("[") || item1.StartsWith("{"))
						{
							var valueToSet = Json.convertStringToList(item1, property.PropertyType);
							object instance = Activator.CreateInstance(property.PropertyType);
							IList list = (IList) instance;
							foreach (object item in valueToSet)
							{
								list.Add(item);
							}
							property.SetValue(this, list, null);
						}
						else
						{
							var valueToSet = Json.convertStringToType(temp, property.PropertyType);
							property.SetValue(this, valueToSet);
						}
					}
				}
			}
			if (helper.subJsons != null)
			{
				foreach (Json subJson in helper.subJsons)
				{
					switch (subJson.head.ToLower())
					{
						case "hull":
							hull = new Hull(subJson);
							break;
						case "turret":
							turret = new Turret(subJson);
							break;
					}
				}
			}
		}
	}
}
