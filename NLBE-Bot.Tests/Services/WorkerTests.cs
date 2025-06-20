namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;

[TestClass]
public class WorkerTests
{
	private ILogger<Worker>? _loggerMock;
	private IServiceProvider? _serviceProviderMock;
	private IPublicIpAddress? _publicIpMock;
	private IBot? _botMock;

	[TestInitialize]
	public void Setup()
	{
		_loggerMock = Substitute.For<ILogger<Worker>>();
		_serviceProviderMock = Substitute.For<IServiceProvider>();
		_publicIpMock = Substitute.For<IPublicIpAddress>();
		_botMock = Substitute.For<IBot>();
	}

	[TestMethod]
	public async Task ExecuteAsync_LogsStartupAndShutdown()
	{
		// Arrange.
		_serviceProviderMock!.GetService(typeof(IBot)).Returns(_botMock);
		_publicIpMock!.GetPublicIpAddressAsync().Returns("1.2.3.4");
		_botMock!.RunAsync().Returns(Task.CompletedTask);

		Worker worker = new(_serviceProviderMock, _loggerMock, _publicIpMock);

		using CancellationTokenSource cts = new();
		cts.CancelAfter(10); // Cancel quickly to exit loop

		// Act.
		await worker.StartAsync(cts.Token);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		_loggerMock!.Received().Log(
		  LogLevel.Information,
		  Arg.Any<EventId>(),
		  Arg.Is<object>(v => v != null && v.ToString().Contains("NLBE Bot is starting.")),
		  null,
		  Arg.Any<Func<object, Exception?, string>>());

		_loggerMock!.Received().Log(
		  LogLevel.Information,
		  Arg.Any<EventId>(),
		  Arg.Is<object>(v => v != null && v.ToString().Contains("NLBE Bot is stopped.")),
		  null,
		  Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CS8602
	}

	[TestMethod]
	public async Task ExecuteAsync_HandlesOperationCanceledException()
	{
		// Arrange.
		_serviceProviderMock!.GetService(typeof(IBot)).Returns(_botMock);
		_publicIpMock!.GetPublicIpAddressAsync().Returns("1.2.3.4");
		_botMock!.RunAsync().ThrowsAsync(new OperationCanceledException());

		Worker worker = new(_serviceProviderMock, _loggerMock, _publicIpMock);

		// Act.
		await worker.StartAsync(CancellationToken.None);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		_loggerMock!.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v != null && v.ToString().Contains("NLBE Bot was cancelled gracefully.")),
			null,
			Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CS8602
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenServiceProviderIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Worker(null, _loggerMock, _publicIpMock));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Worker(_serviceProviderMock, null, _publicIpMock));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenPublicIpAddressIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Worker(_serviceProviderMock, _loggerMock, null));
	}
}
