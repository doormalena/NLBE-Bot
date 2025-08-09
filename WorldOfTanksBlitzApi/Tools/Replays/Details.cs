using JsonObjectConverter;
using System;
using System.Collections.Generic;

namespace FMWOTB.Tools.Replays
{

	public class Details
	{
		public int damage_assisted_track
		{
			get; private set;
		}
		public int base_capture_points
		{
			get; private set;
		}
		public int wp_points_earned
		{
			get; private set;
		}
		public long time_alive
		{
			get; private set;
		}
		public long chassis_id
		{
			get; private set;
		}
		public int hits_received
		{
			get; private set;
		}
		public int shots_splash
		{
			get; private set;
		}
		public long gun_id
		{
			get; private set;
		}
		public int hits_pen
		{
			get; private set;
		}
		public int hero_bonus_credits
		{
			get; private set;
		}
		public int hitpoints_left
		{
			get; private set;
		}
		public long dbid
		{
			get; private set;
		}
		public int shots_pen
		{
			get; private set;
		}
		public int exp_for_assist
		{
			get; private set;
		}
		public int damage_received
		{
			get; private set;
		}
		public int hits_bounced
		{
			get; private set;
		}
		public int hero_bonus_exp
		{
			get; private set;
		}
		public int enemies_damaged
		{
			get; private set;
		}
		public IReadOnlyCollection<Achievement> achievements
		{
			get; private set;
		}
		public int exp_for_damage
		{
			get; private set;
		}
		public int damage_blocked
		{
			get; private set;
		}
		public int distance_travelled
		{
			get; private set;
		}
		public int hits_splash
		{
			get; private set;
		}
		public int credits
		{
			get; private set;
		}
		public int squad_index
		{
			get; private set;
		}
		public int wp_points_stolen
		{
			get; private set;
		}
		public int damage_made
		{
			get; private set;
		}
		public int vehicle_descr
		{
			get; private set;
		}
		public int exp_team_bonus
		{
			get; private set;
		}
		public int enemies_spotted
		{
			get; private set;
		}
		public int shots_hit
		{
			get; private set;
		}
		public long clanid
		{
			get; private set;
		}
		public long turret_id
		{
			get; private set;
		}
		public int enemies_destroyed
		{
			get; private set;
		}
		public long killed_by
		{
			get; private set;
		}
		public int base_defend_points
		{
			get; private set;
		}
		public int exp
		{
			get; private set;
		}
		public int damage_assisted
		{
			get; private set;
		}
		public int death_reason
		{
			get; private set;
		}
		public int shots_made
		{
			get; private set;
		}
		public string clan_tag
		{
			get; private set;
		}

		public Details(Json json)
		{
			if (json.tupleList != null)
			{
				foreach (var subHelper in json.tupleList)
				{
					string temp = subHelper.Item2.Item1.Trim(' ').Trim('\"');
					if (!temp.ToLower().Equals("null"))
					{
						var property = this.GetType().GetProperty(subHelper.Item1.Trim(' ').Trim('\"').ToLower());
						if (property != null)
						{
							var valueToSet = Convert.ChangeType(temp, property.PropertyType);
							property.SetValue(this, valueToSet);
						}
					}
				}
			}
			if (json.subJsons != null)
			{
				List<Achievement> achList = new List<Achievement>();
				foreach (Json subHelper in json.subJsons)
				{
					achList.Add(new Achievement(subHelper));
				}
				this.achievements = achList;
				//this.achievements = achList.OrderBy(x => x.t).ThenBy(y => y.v).ToList();
			}
		}
	}
}
