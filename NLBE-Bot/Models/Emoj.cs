namespace NLBE_Bot.Models;

public static class Emoj
{
	public static string GetName(int index)
	{
		return index switch
		{
			1 => ":one:",
			2 => ":two:",
			3 => ":three:",
			4 => ":four:",
			5 => ":five:",
			6 => ":six:",
			7 => ":seven:",
			8 => ":eight:",
			9 => ":nine:",
			10 => ":keycap_ten:",
			_ => string.Empty,
		};
	}

	public static int GetIndex(string name)
	{
		return name switch
		{
			":one:" => 1,
			":two:" => 2,
			":three:" => 3,
			":four:" => 4,
			":five:" => 5,
			":six:" => 6,
			":seven:" => 7,
			":eight:" => 8,
			":nine:" => 9,
			":keycap_ten:" => 10,
			_ => 0,
		};
	}
}
