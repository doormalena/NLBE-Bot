namespace WorldOfTanksBlitzApi.Interfaces;

using System.Threading.Tasks;

public interface IWotInspectorConnection
{
	public Task<string> UploadReplayAsync(string relativeUrl, string fileName, byte[] fileContent, string? title, long? accountId);
}
