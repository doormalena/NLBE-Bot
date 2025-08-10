using JsonObjectConverter;
using System.Collections.Generic;

namespace WorldOfTanksBlitzApi.Vehicles.Default_profile
{
	public class default_profile
	{
		public int battle_level_range_max
		{
			get; private set;
		}
		public int battle_level_range_min
		{
			get; private set;
		}
		public int engine_id
		{
			get; private set;
		}
		public int firepower
		{
			get; private set;
		}
		public int gun_id
		{
			get; private set;
		}
		public int hp
		{
			get; private set;
		}
		public int hull_hp
		{
			get; private set;
		}
		public int hull_weight
		{
			get; private set;
		}
		public bool is_default
		{
			get; private set;
		}
		public int maneuverability
		{
			get; private set;
		}
		public int max_ammo
		{
			get; private set;
		}
		public int max_weight
		{
			get; private set;
		}
		public string profile_id
		{
			get; private set;
		}
		public int protection
		{
			get; private set;
		}
		public int shot_efficiency
		{
			get; private set;
		}
		public int signal_range
		{
			get; private set;
		}
		public int speed_backward
		{
			get; private set;
		}
		public int speed_forward
		{
			get; private set;
		}
		public int suspension_id
		{
			get; private set;
		}
		public int turret_id
		{
			get; private set;
		}
		public int weight
		{
			get; private set;
		}
		public Armor armor
		{
			get; private set;
		}
		public Engine engine
		{
			get; private set;
		}
		public Gun gun
		{
			get; private set;
		}
		public List<Shell> shells
		{
			get; private set;
		}

		public default_profile(Json json)
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
					switch (subJson.head.ToLower())
					{
						case "armor":
							armor = new Armor(subJson);
							break;
						case "engine":
							engine = new Engine(subJson);
							break;
						case "gun":
							gun = new Gun(subJson);
							break;
						case "shells":
							if (shells == null)
							{
								shells = new List<Shell>();
							}
							shells.Add(new Shell(subJson));
							break;
					}
				}
			}
		}
	}
}
