namespace NLBE_Bot.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IApiRequester
{
	public Task<string> GetRequest(string url, Dictionary<string, string> parameters = null);
}
