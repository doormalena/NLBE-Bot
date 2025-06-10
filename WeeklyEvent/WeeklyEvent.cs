using DiscordHelper;
using DSharpPlus.Entities;
using NLBE_Bot.Models;
using NLBE_Bot.Services;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NLBE_Bot
{
    public class WeeklyEvent
    {
        public string Tank { get; private set; }
        public List<WeeklyEventItem> WeeklyEventItems { get; set; }
        public DateTime StartDate { get; set; }

        private static string DATETIME_FORMAT = "dd MMMM HHu";
        private static string DATE_RANGE_SPLITTER = " tot ";

        public WeeklyEvent(string tank, List<WeeklyEventItem> weeklyEventItems)
        {
            this.Tank = tank;
            this.WeeklyEventItems = weeklyEventItems;
            StartDate = StartOfWeek(DateTime.Now);
        }

        public WeeklyEvent(DiscordMessage message)
        {
            Tank = message.Embeds[0].Title;
            WeeklyEventItems = new List<WeeklyEventItem>();
            foreach (DiscordEmbedField embedField in message.Embeds[0].Fields)
            {
                WeeklyEventItems.Add(new WeeklyEventItem(embedField));
            }
            StartDate = StartOfWeek(message.CreationTimestamp.DateTime);
        }

        public DiscordEmbed GenerateEmbed()
        {
            try
            {
                DiscordEmbedBuilder newDiscEmbedBuilder = new DiscordEmbedBuilder();
                newDiscEmbedBuilder.Color = Constants.WEEKLY_EVENT_COLOR;
                string description = StartDate.ToString(DATETIME_FORMAT) + DATE_RANGE_SPLITTER + GetEndDate().ToString(DATETIME_FORMAT).TrimStart('0');
                newDiscEmbedBuilder.Description = description;

                foreach (WeeklyEventItem weeklyEventItem in WeeklyEventItems)
                {
                    DEF def = weeklyEventItem.GenerateDEF();
                    newDiscEmbedBuilder.AddField(def.Name, def.Value, def.Inline);
                }

                newDiscEmbedBuilder.Title = this.Tank.Replace("\\", string.Empty);

                return newDiscEmbedBuilder.Build();
            }
            catch (Exception e)
            {
                Bot.HandleError("While editing HOF message: ", e.Message, e.StackTrace).Wait();
            }
            return null;
        }

        public DateTime GetEndDate()
        {
            return this.StartDate.AddDays(7);
        }

        public static DateTime StartOfWeek(DateTime dt)
        {
            dt = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0 , 0);
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            dt = dt.AddDays(-1 * diff).Date;
            dt = dt.AddHours(14);
            return dt;
        }
    }
}
