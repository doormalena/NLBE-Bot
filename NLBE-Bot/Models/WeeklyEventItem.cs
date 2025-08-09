namespace NLBE_Bot.Models;

using DSharpPlus.Entities;
using System;

public class WeeklyEventItem
{
	public int Value
	{
		get; set;
	}

	public string Player
	{
		get; set;
	}

	public string Url
	{
		get; set;
	}

	public WeeklyEventType WeeklyEventType
	{
		get;
	}

	public WeeklyEventItem(WeeklyEventType WeeklyEventType)
	{
		Reset();
		this.WeeklyEventType = WeeklyEventType;
	}

	public WeeklyEventItem(int value, string player, string url, WeeklyEventType weeklyEventType)
	{
		Value = value;
		Player = player;
		Url = url;
		WeeklyEventType = weeklyEventType;
	}

	public WeeklyEventItem(DiscordEmbedField embedField)
	{
		foreach (WeeklyEventType enumerableItem in Enum.GetValues<WeeklyEventType>())
		{
			if (embedField.Name == enumerableItem.ToString().Replace('_', ' ') + ":")
			{
				string[] splitted = embedField.Value.Split(')');

				if (splitted.Length > 1)
				{
					string[] splitted2 = splitted[0].Split('(');
					Url = splitted2[1];
					Player = splitted2[0].TrimStart('[').TrimEnd(']');
					Value = int.Parse(splitted[1].Replace("`", string.Empty).Trim().Split(' ')[0]);
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
		DEF def = new()
		{
			Inline = false,
			Name = WeeklyEventType.ToString().Replace('_', ' ') + ":",

			Value = Player.Length > 0
				? "[" + Player + "](" + Url + ") `" + Value + "` " + WeeklyEventType.ToString().Replace("Most_", string.Empty).Replace('_', ' ')
				: "Bevat nog geen top score."
		};

		return def;
	}

	public void Reset()
	{
		Value = 0;
		Player = string.Empty;
		Url = string.Empty;
	}
}
