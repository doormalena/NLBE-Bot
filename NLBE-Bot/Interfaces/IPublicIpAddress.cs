namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;

internal interface IPublicIpAddress
{
	public Task<string> GetPublicIpAddressAsync();
}
