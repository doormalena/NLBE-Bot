namespace NLBE_Bot.Blitzstars;

using System.Collections.Generic;

public class PlayerTankAndTankHistory
{
	public PlayerVehicle.Vehicle PlayerTank
	{
		get; set;
	}
	public List<TankHistory> TnkHistoryList
	{
		get; set;
	}
}
