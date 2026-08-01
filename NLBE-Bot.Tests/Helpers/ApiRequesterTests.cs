namespace NLBE_Bot.Tests.Helpers;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Helpers;
using RichardSzalay.MockHttp;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

[TestClass]
public class ApiRequesterTests
{
	private MockHttpMessageHandler? _mockHttp;
	private HttpClient? _client;
	private ApiRequester? _requester;

	[TestInitialize]
	public void Setup()
	{
		_mockHttp = new MockHttpMessageHandler();
		_client = new HttpClient(_mockHttp);
		_requester = new ApiRequester(_client);
	}

	[TestMethod]
	public async Task GetRequest_AddsHeadersAndReturnsContent()
	{
		// Arrange.
		string url = "https://example.test/api";
		_mockHttp!.When(HttpMethod.Get, url)
			.WithHeaders("X-Api-Key", "abc123")
			.Respond("text/plain", "response body");

		Dictionary<string, string> parameters = new()
		{ { "X-Api-Key", "abc123" } };

		// Act.
		string result = await _requester!.GetRequest(url, parameters);

		// Assert.
		Assert.AreEqual("response body", result);
	}

	[TestMethod]
	public async Task GetRequest_NoParameters_ReturnsContent()
	{
		// Arrange.
		string url = "https://example.test/other";
		_mockHttp!.When(HttpMethod.Get, url).Respond("text/plain", "ok");

		// Act.
		string result = await _requester!.GetRequest(url);

		// Assert.
		Assert.AreEqual("ok", result);
	}
}
