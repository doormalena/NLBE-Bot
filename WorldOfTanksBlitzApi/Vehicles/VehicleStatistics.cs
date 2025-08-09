using JsonObjectConverter;
using System;
using System.Collections;

namespace FMWOTB.Vehicles
{
	public class VehicleStatistics
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
		public int max_xp
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

		public VehicleStatistics(Json json)
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
