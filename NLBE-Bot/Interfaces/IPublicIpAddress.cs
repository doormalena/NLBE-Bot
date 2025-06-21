namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;

public interface IPublicIpAddress
{
	public Task<string> GetPublicIpAddressAsync();
}
