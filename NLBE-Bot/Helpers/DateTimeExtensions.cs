namespace NLBE_Bot.Helpers;

using System;
using System.Text;

public static class DateTimeExtensions
{
	public static string ConvertToDate(this DateTimeOffset date)
	{
		string theDate = date.Day.ToString() + "-" + (date.Month < 10 ? "0" : "") + date.Month.ToString() + "-" + date.Year.ToString() + " " + date.Hour.ToString() + ":" + (date.Minute < 10 ? "0" : "") + date.Minute.ToString() + ":" + (date.Second < 10 ? "0" : "") + date.Second.ToString();
		return ConvertToDate(theDate);
	}

	public static string ConvertToDate(this DateTime dateTime)
	{
		string theDate = dateTime.Day.ToString() + "-" + (dateTime.Month < 10 ? "0" : "") + dateTime.Month.ToString() + "-" + dateTime.Year.ToString() + " " + dateTime.Hour.ToString() + ":" + (dateTime.Minute < 10 ? "0" : "") + dateTime.Minute.ToString() + ":" + (dateTime.Second < 10 ? "0" : "") + dateTime.Second.ToString();
		return ConvertToDate(theDate);
	}

	public static DateTime ConvertToDateTime(this DateTime dateTime)
	{
		return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, DateTimeKind.Local);
	}

	private static string ConvertToDate(this string date)
	{
		string[] splitted = date.Replace('/', '-').Split(' ');
		StringBuilder sb = new();
		for (int i = 0; i < splitted.Length; i++)
		{
			if (i < 2)
			{
				if (i > 0)
				{
					sb.Append(' ');
				}
				sb.Append(splitted[i]);
			}
		}
		return sb.ToString();
	}

	public static bool CompareDateTime(this DateTime x, DateTime y)
	{
		TimeSpan tempTimeSpan = x.Subtract(y);
		if (tempTimeSpan.Hours.Equals(0) && tempTimeSpan.Minutes.Equals(0) && tempTimeSpan.Seconds.Equals(0) && x.Year.Equals(y.Year) && x.Month.Equals(y.Month) && x.Day.Equals(y.Day))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
}
