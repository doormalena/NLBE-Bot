namespace NLBE_Bot.Blitzstars {
    public class TankHistory {
        public string _id { get; set; }
        public long last_battle_time { get; set; }
        public long account_id { get; set; }
        public long in_garage_updated { get; set; }
        public object frags { get; set; }
        public int mark_of_mastery { get; set; }
        public int battle_life_time { get; set; }
        public object in_garage { get; set; }
        public int tank_id { get; set; }
        public string region { get; set; }
        public double wn7 { get; set; }
        public double wn8 { get; set; }
        public int __v { get; set; }
        public All all { get; set; }
        public int? max_xp { get; set; }
        public int? max_frags { get; set; }
        public int? tier { get; set; }
        public int? update_type { get; set; }
        public TankHistory start_record { get; set; }
        public TankHistory end_record { get; set; }
        public long start_date { get; set; }
        public long end_date { get; set; }

        public class All
        {
            public int spotted { get; set; }
            public int hits { get; set; }
            public int frags { get; set; }
            public int max_xp { get; set; }
            public int wins { get; set; }
            public int losses { get; set; }
            public int capture_points { get; set; }
            public int battles { get; set; }
            public int damage_dealt { get; set; }
            public int damage_received { get; set; }
            public int max_frags { get; set; }
            public int shots { get; set; }
            public int frags8p { get; set; }
            public int xp { get; set; }
            public int win_and_survived { get; set; }
            public int survived_battles { get; set; }
            public int dropped_capture_points { get; set; }
        }
        public TankHistory(PlayerVehicle.Vehicle v)
        {
            account_id = v.account_id;
            last_battle_time = v.last_battle_time;
            in_garage_updated = v.in_garage_updated;
            frags = v.frags;
            mark_of_mastery = (int)v.mark_of_mastery;
            battle_life_time = (int)v.battle_life_time;
            in_garage = v.in_garage;
            tank_id = v.tank_id;
            max_xp = v.max_xp;
            max_frags = v.max_frags;
            all = new All();
            if (v.all != null){
                all.spotted = v.all.spotted;
                all.hits = v.all.hits;
                all.frags = v.all.frags;
                all.max_xp = v.all.max_xp;
                all.wins = v.all.wins;
                all.losses = v.all.losses;
                all.capture_points = v.all.capture_points;
                all.battles = v.all.battles;
                all.damage_dealt = v.all.damage_dealt;
                all.damage_received = v.all.damage_received;
                all.max_frags = v.all.max_frags;
                all.shots = v.all.shots;
                all.frags8p = v.all.frags8p;
                all.xp = v.all.xp;
                all.win_and_survived = v.all.win_and_survived;
                all.survived_battles = v.all.survived_battles;
                all.dropped_capture_points = v.all.dropped_capture_points;
            }
        }
        public TankHistory() {}
    }
}
