namespace NLBE_Bot.Blitzstars;

using System.Collections.Generic;

public class PlayerVehicle
{
	public string status
	{
		get; set;
	}
	public Meta meta
	{
		get; set;
	}
	public Data data
	{
		get; set;
	}
	public class Meta
	{
		public int count
		{
			get; set;
		}
	}
	public class Data
	{
		public List<Vehicle> Vehicles
		{
			get; set;
		}
	}	
}
