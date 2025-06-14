namespace NLBE_Bot.Models;

public class DEF
{
	public string Name
	{
		get; set;
	}

	public string Value
	{
		get; set;
	}

	public bool Inline
	{
		get; set;
	}

	public DEF()
	{
		Inline = false;
	}
}
