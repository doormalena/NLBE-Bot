namespace NLBE_Bot;

public class TankHof(string link, string speler, string tank, int damage, int tier)
{
	public string Link
	{
		get;
	} = link;
	public string Speler
	{
		get; set;
	} = speler;
	public string Tank
	{
		get;
	} = tank;
	public int Damage
	{
		get;
	} = damage;
	public int Tier
	{
		get;
	} = tier;
	public short Place { get; set; } = 1;
}
