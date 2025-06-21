namespace NLBE_Bot;

using DSharpPlus.Entities;
using System.Reflection;

public static class Constants
{
	public const char LOG_SPLIT_CHAR = '|';
	public const string Prefix = "nlbe ";
	public const string ERROR_REACTION = ":x:";
	public const string IN_PROGRESS_REACTION = ":hourglass_flowing_sand:";
	public const string ACTION_COMPLETED_REACTION = ":white_check_mark:";
	public const string MAINTENANCE_REACTION = ":tool_logo:";
	public const int HOF_AMOUNT_PER_TANK = 3;
	public const ulong NLBE_SERVER_ID = 507575681593638913;
	public const ulong DA_BOIS_ID = 693519504235561080;
	public const ulong MOET_REGELS_NOG_LEZEN_ROLE = 793830434551103500;
	public const ulong NOOB_ROLE = 782272112505258054;
	public const ulong LEDEN_ROLE = 681965919614009371;
	public const ulong NLBE_ROLE = 668534098729631745;
	public const ulong NLBE2_ROLE = 781625012695728140;
	public const ulong TOERNOOI_DIRECTIE = 782751703559700530;
	public const ulong DISCORD_ADMIN_ROLE = 781634960930242614;
	public const ulong DEPUTY_ROLE = 557951586975088662;
	public const ulong DEPUTY_NLBE_ROLE = 805783688783724604;
	public const ulong DEPUTY_NLBE2_ROLE = 805783828312227840;
	public const ulong BEHEERDER_ROLE = 681865080803033109;
	public const ulong NLBE_BOT = 781618903314202644;
	public const ulong TESTBEASTV2_BOT = 794166024135639050;
	public const ulong THIBEASTMO_ID = 239109910321823744;
	public const ulong THIBEASTMO_ALT_ID = 756193463913021512;
	public const ulong MASTERY_REPLAYS_ID = 734359875253174323;
	public const ulong BOTTEST_ID = 781617141069774898;
	public const ulong PRIVE_ID = 702607178892312587;
	public const ulong NLBE_TOERNOOI_AANMELDEN_KANAAL_ID = 714860361894854780;
	public const ulong DA_BOIS_TOERNOOI_AANMELDEN_KANAAL_ID = 808324144197271573;
	public const long NLBE_CLAN_ID = 865;
	public const long NLBE2_CLAN_ID = 48814;
	public const char UNDERSCORE_REPLACEMENT_CHAR = 'ˍ';
	public const char REPLACEABLE_UNDERSCORE_CHAR = '＿';

	public static readonly DiscordColor WEEKLY_EVENT_COLOR = DiscordColor.Gold;
	public static readonly DiscordColor HOF_COLOR = DiscordColor.Blurple;
	public static readonly DiscordColor BOT_COLOR = DiscordColor.Red;

	public static string Version =>
			Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
			?? "Unknown";
}
