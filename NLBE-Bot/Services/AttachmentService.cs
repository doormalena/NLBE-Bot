namespace NLBE_Bot.Services;

using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class AttachmentService(HttpClient httpClient, ILogger<AttachmentService> logger) : IAttachmentService
{
	private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	private readonly ILogger<AttachmentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async Task<(string FileName, byte[] Content)> DownloadAttachmentAsync(IDiscordAttachment attachment)
	{
		string fileName = Path.GetFileName(attachment.Url);
		_logger.LogDebug("Downloading attachment from {Url}", attachment.Url);

		byte[] content = await _httpClient.GetByteArrayAsync(attachment.Url);
		return (fileName, content);
	}
}
