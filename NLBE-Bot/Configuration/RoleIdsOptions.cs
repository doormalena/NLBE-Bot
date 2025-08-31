namespace NLBE_Bot.Configuration;
using System.ComponentModel.DataAnnotations;

public class RoleIdsOptions
{
	[Required]
	public ulong Noob
	{
		get; set;
	}

	[Required]
	public ulong Members
	{
		get; set;
	}

	[Required]
	public ulong MustReadRules
	{
		get; set;
	}
}
