namespace WorldOfTanksBlitzApi.Tests.Integration;

using Microsoft.Extensions.Logging;
using NSubstitute;

[TestClass, Ignore("Only use locally to test integration with real API")]
public class WotbConnectionRequestTests
{
	private HttpClient? _httpClient;
	private ILogger<WotbConnection>? _loggerMock;
	private WotbConnection? _connection;
	private readonly string BaseUri = "https://api.wotblitz.eu/wotb";
	private readonly string ApplicationId = "app_id"; // Replace with your actual application ID

	[TestInitialize]
	public void Setup()
	{
		_httpClient = new HttpClient();
		_loggerMock = Substitute.For<ILogger<WotbConnection>>();
		_connection = new(_httpClient, _loggerMock, BaseUri, ApplicationId);
	}

	[TestMethod]
	public async Task PostAsync_ReturnsContent_WhenReachedRateLimit()
{
		// Arrange.
		const string relativeUrl = "/account/list/";

		static MultipartFormDataContent CreateForm()
		{
			MultipartFormDataContent form = [];
			form.Add(new StringContent("Kqb658kbgy"), "search");
			form.Add(new StringContent("20"), "limit");
			form.Add(new StringContent("exact"), "type");
			return form;
		}

		int totalRequests = 30; // Exceeds 20/sec limit
		List<Task<string>> tasks = [];

		// Act.
		for (int i = 0; i < totalRequests; i++)
		{
			tasks.Add(_connection!.PostAsync(relativeUrl, CreateForm()));
		}

		string[] responses = await Task.WhenAll(tasks);

		// Assert.
		foreach (string content in responses)
		{
			Assert.IsFalse(string.IsNullOrWhiteSpace(content), "Response content should not be empty or whitespace.");
			Assert.IsTrue(content.Contains("\"status\":\"ok\""), "Response should indicate success with status 'ok'.");
		}

		_loggerMock!.Received().Log(
			LogLevel.Debug,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Rate limit exceeded. Retrying in")),
			null,
			Arg.Any<Func<object, Exception?, string>>());
	}
}
