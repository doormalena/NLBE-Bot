namespace NLBE_Bot;

using DSharpPlus.Entities;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;

public class WeeklyEvent
{
	public string Tank
	{
		get; private set;
	}
	public List<WeeklyEventItem> WeeklyEventItems
	{
		get; set;
	}
	public DateTime StartDate
	{
		get; set;
	}

	private const string DATETIME_FORMAT = "dd MMMM HHu";
	private const string DATE_RANGE_SPLITTER = " tot ";

	public WeeklyEvent(string tank, List<WeeklyEventItem> weeklyEventItems)
	{
		Tank = tank;
		WeeklyEventItems = weeklyEventItems;
		StartDate = StartOfWeek(DateTime.Now);
	}

	public WeeklyEvent(DiscordMessage message)
	{
		Tank = message.Embeds[0].Title;
		WeeklyEventItems = [];
		foreach (DiscordEmbedField embedField in message.Embeds[0].Fields)
		{
			WeeklyEventItems.Add(new WeeklyEventItem(embedField));
		}
		StartDate = StartOfWeek(message.CreationTimestamp.DateTime);
	}

	public DiscordEmbed GenerateEmbed()
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = Constants.WEEKLY_EVENT_COLOR
		};
		string description = StartDate.ToString(DATETIME_FORMAT) + DATE_RANGE_SPLITTER + GetEndDate().ToString(DATETIME_FORMAT).TrimStart('0');
		newDiscEmbedBuilder.Description = description;

		foreach (WeeklyEventItem weeklyEventItem in WeeklyEventItems)
		{
			DEF def = weeklyEventItem.GenerateDEF();
			newDiscEmbedBuilder.AddField(def.Name, def.Value, def.Inline);
		}

		newDiscEmbedBuilder.Title = Tank.Replace("\\", string.Empty);

		return newDiscEmbedBuilder.Build();
	}

	public DateTime GetEndDate()
	{
		return StartDate.AddDays(7);
	}

	public static DateTime StartOfWeek(DateTime dt)
	{
		dt = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
		int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
		dt = dt.AddDays(-1 * diff).Date;
		dt = dt.AddHours(14);
		return dt;
	}
}
