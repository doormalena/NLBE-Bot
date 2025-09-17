namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using RichardSzalay.MockHttp;

[TestClass]
public class AttachmentServiceTests
{
	private MockHttpMessageHandler? _mockHttp;
	private HttpClient? _httpClient;
	private ILogger<AttachmentService>? _loggerMock;
	private AttachmentService? _service;

	[TestInitialize]
	public void Setup()
	{
		_mockHttp = new();
		_httpClient = _mockHttp.ToHttpClient();
		_loggerMock = Substitute.For<ILogger<AttachmentService>>();
		_service = new(_httpClient, _loggerMock);
	}

	[TestMethod]
	public async Task DownloadAttachmentAsync_ReturnsFileNameAndContent()
	{
		// Arrange.
		string attachmentUrl = "https://cdn.discordapp.com/attachments/123/test.wotbreplay";
		byte[] expectedBytes = [1, 2, 3, 4];
		_mockHttp!.When(HttpMethod.Get, attachmentUrl)
				 .Respond(req => new HttpResponseMessage { Content = new ByteArrayContent([1, 2, 3, 4]) });

		IDiscordAttachment attachment = Substitute.For<IDiscordAttachment>();
		attachment.Url.Returns(attachmentUrl);

		// Act.
		(string fileName, byte[] content) = await _service!.DownloadAttachmentAsync(attachment);

		// Assert.
		Assert.AreEqual("test.wotbreplay", fileName);
		CollectionAssert.AreEqual(expectedBytes, content);
	}
}
