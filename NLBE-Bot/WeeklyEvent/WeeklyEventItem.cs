using DSharpPlus.Entities;
using NLBE_Bot.Models;
using System;

namespace NLBE_Bot
{
    public class WeeklyEventItem
    {
        public int Value { get; private set; }
        public string Player { get; private set; }
        public string Url { get; private set; }
        public WeeklyEventType WeeklyEventType { get; }

        public WeeklyEventItem(WeeklyEventType WeeklyEventType)
        {
            Reset();
            this.WeeklyEventType = WeeklyEventType;
        }
        public WeeklyEventItem(int value, string player, string url, WeeklyEventType weeklyEventType)
        {
            this.Value = value;
            this.Player = player;
            this.Url = url;
            this.WeeklyEventType = weeklyEventType;
        }

        public WeeklyEventItem(DiscordEmbedField embedField)
        {
            foreach (var enumerableItem in Enum.GetValues(typeof(WeeklyEventType))) {
                if (embedField.Name == enumerableItem.ToString().Replace('_', ' ') + ":")
                {
                    WeeklyEventType = (WeeklyEventType)enumerableItem;
                    string[] splitted = embedField.Value.Split(')');
                    if (splitted.Length > 1)
                    {
                        string[] splitted2 = splitted[0].Split('(');
                        Url = splitted2[1];
                        Player = splitted2[0].TrimStart('[').TrimEnd(']');
                        Value = Int32.Parse(splitted[1].Replace("`", string.Empty).Trim().Split(' ')[0]);
                    }
                    else
                    {
                        Reset();
                    }
                    break;
                }
            }
        }

        public DEF GenerateDEF()
        {
            DEF def = new DEF();
            def.Inline = false;
            def.Name = WeeklyEventType.ToString().Replace('_', ' ') + ":";
            if (Player.Length > 0)
            {
                def.Value = "[" + Player + "](" + Url + ") `" + Value + "` " + WeeklyEventType.ToString().Replace("Most_", string.Empty).Replace('_', ' ');
            }
            else
            {
                def.Value = "Bevat nog geen top score.";
            }
            return def;
        }

        public void Reset()
        {
            this.Value = 0;
            this.Player = string.Empty;
            this.Url = string.Empty;
        }
    }
}
