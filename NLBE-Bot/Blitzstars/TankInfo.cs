namespace NLBE_Bot.Blitzstars {
    public class TankInfo {
        public TankHistory[] History { get; set; }
        public TankHistory Period30d { get; set; }
        public TankHistory Period60d { get; set; }
        public TankHistory Period90d { get; set; }
        public TankHistory Live { get; set; }
    }
}
