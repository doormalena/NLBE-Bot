namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;

public interface IBlitzstarsService
{
	public Task<int> Get90DayBattles(long accountId);
}
