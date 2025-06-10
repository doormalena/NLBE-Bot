using System.Collections.Generic;
namespace NLBE_Bot.Blitzstars {
    public class PlayerVehicle {
        public string status { get; set; }
        public Meta meta { get; set; }
        public Data data { get; set; }
        public class Meta
        {
            public int count { get; set; }
        }
        public class Data
        {
            public List<Vehicle> Vehicles { get; set; }
        }

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

        public class Vehicle {
            public All all { get; set; }
            public int last_battle_time { get; set; }
            public int account_id { get; set; }
            public int max_xp { get; set; }
            public int in_garage_updated { get; set; }
            public int max_frags { get; set; }
            public string frags { get; set; }
            public int mark_of_mastery { get; set; }
            public int battle_life_time { get; set; }
            public string in_garage { get; set; }
            public int tank_id { get; set; }
        }
    }
}
