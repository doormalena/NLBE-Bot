using JsonObjectConverter;
using System;
using System.Collections;

namespace WorldOfTanksBlitzApi.Vehicles.Default_profile
{
	public class Shell
	{
		public int damage
		{
			get; private set;
		}
		public int penetration
		{
			get; private set;
		}
		public string type
		{
			get; private set;
		}

		public Shell(Json json)
		{
			setValues(json);
		}
		private void setValues(Json helper)
		{
			if (helper.subJsons != null)
			{
				foreach (Json json in helper.subJsons)
				{
					if (helper.tupleList != null)
					{
						foreach (var tuple in json.tupleList)
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
				}
			}
		}
	}
}
