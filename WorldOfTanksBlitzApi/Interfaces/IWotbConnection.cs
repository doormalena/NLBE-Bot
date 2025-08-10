namespace WorldOfTanksBlitzApi.Interfaces;

using System.Net.Http;
using System.Threading.Tasks;

public interface IWotbConnection
{
	public Task<string> PostAsync(string relativeUrl, MultipartFormDataContent form);
}
