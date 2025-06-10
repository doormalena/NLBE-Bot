using System;
namespace NLBE_Bot.Blitzstars {
    public class Tank {
        public All all { get; set; }
        public string _id { get; set; }
        public int last_battle_time { get; set; }
        public int account_id { get; set; }
        public int in_garage_updated { get; set; }
        public object frags { get; set; }
        public int mark_of_mastery { get; set; }
        public int battle_life_time { get; set; }
        public object in_garage { get; set; }
        public int tank_id { get; set; }
        public string region { get; set; }
        public double wn7 { get; set; }
        public double wn8 { get; set; }
        public int __v { get; set; }
        public int? update_type { get; set; }
        public DateTime? createdAt { get; set; }
        public DateTime? updatedAt { get; set; }
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
}
