namespace NLBE_Bot.Interfaces;

using System.Collections.Generic;

public interface IApiRequester
{
	public string GetRequest(string url, Dictionary<string, string> parameters = null);
}
