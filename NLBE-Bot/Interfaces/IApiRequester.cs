namespace NLBE_Bot.Interfaces;

using System.Collections.Generic;

internal interface IApiRequester
{
	public string GetRequest(string url, Dictionary<string, string> parameters = null);
}
