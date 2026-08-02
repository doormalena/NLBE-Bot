namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;

internal interface IAttachmentService
{
	public Task<(string FileName, byte[] Content)> DownloadAttachmentAsync(IDiscordAttachment attachment);
}
