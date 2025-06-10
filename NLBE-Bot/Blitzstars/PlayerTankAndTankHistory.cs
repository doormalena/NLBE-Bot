using System.Collections.Generic;
namespace NLBE_Bot.Blitzstars {
    public class PlayerTankAndTankHistory {
        public PlayerVehicle.Vehicle PlayerTank { get; set; }
        public List<TankHistory> TnkHistoryList { get; set; }
    }
}
