using JsonObjectConverter;

namespace WorldOfTanksBlitzApi
{
	public class Option
	{
		public string description
		{
			get; private set;
		}
		public string image
		{
			get; private set;
		}
		public string image_big
		{
			get; private set;
		}
		public string name
		{
			get; private set;
		}

		public Option(Json json)
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
							var valueToSet = Json.convertStringToType(temp, property.PropertyType);
							property.SetValue(this, valueToSet);
						}
					}
				}
			}
		}
	}
}
