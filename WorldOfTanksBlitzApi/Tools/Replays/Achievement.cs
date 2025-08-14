using JsonObjectConverter;
using System;

namespace WorldOfTanksBlitzApi.Tools.Replays
{

	public class Achievement
	{
		public int t
		{
			get; private set;
		}
		public int v
		{
			get; private set;
		}

		public Achievement(Json json)
		{
			foreach (var tuple in json.tupleList)
			{
				string temp = tuple.Item2.Item1.Trim(' ').Trim('\"');
				if (!temp.ToLower().Equals("null"))
				{
					var property = this.GetType().GetProperty(tuple.Item1.Trim(' ').Trim('\"').ToLower());
					if (property != null)
					{
						var valueToSet = Convert.ChangeType(temp, property.PropertyType);
						property.SetValue(this, valueToSet);
					}
				}
			}
		}
	}
}
