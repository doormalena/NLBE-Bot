namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Moq;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using System;

[TestClass]
public class WorkerTests
{
	private Mock<ILogger<Worker>>? _loggerMock;
	private Mock<IServiceProvider>? _serviceProviderMock;
	private Mock<IPublicIpAddress>? _publicIpMock;
	private Mock<IBot>? _botMock;

	[TestInitialize]
	public void Setup()
	{
		_loggerMock = new Mock<ILogger<Worker>>();
		_serviceProviderMock = new Mock<IServiceProvider>();
		_publicIpMock = new Mock<IPublicIpAddress>();
		_botMock = new Mock<IBot>();
	}

	[TestMethod]
	public async Task ExecuteAsync_LogsStartupAndShutdown()
	{
		// Arrange
		_serviceProviderMock!.Setup(sp => sp.GetService(typeof(IBot))).Returns(_botMock!.Object);
		_publicIpMock!.Setup(m => m.GetPublicIpAddressAsync()).ReturnsAsync("1.2.3.4");
		_botMock!.Setup(b => b.RunAsync()).Returns(Task.CompletedTask);

		Worker worker = new(_serviceProviderMock.Object, _loggerMock!.Object, _publicIpMock.Object);

		using CancellationTokenSource cts = new();
		cts.CancelAfter(10); // Cancel quickly to exit loop

		// Act.
		await worker.StartAsync(cts.Token);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		_loggerMock.Verify(l => l.Log(
		  LogLevel.Information,
		  It.IsAny<EventId>(),
		  It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("NLBE Bot is starting.")),
		  null,
		  It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

		_loggerMock.Verify(l => l.Log(
		  LogLevel.Information,
		  It.IsAny<EventId>(),
		  It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("NLBE Bot is stopped.")),
		  null,
		  It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CS8602
	}

	[TestMethod]
	public async Task ExecuteAsync_HandlesOperationCanceledException()
	{
		// Arrange
		_serviceProviderMock!.Setup(sp => sp.GetService(typeof(IBot))).Returns(_botMock!.Object);
		_publicIpMock!.Setup(m => m.GetPublicIpAddressAsync()).ReturnsAsync("1.2.3.4");
		_botMock.Setup(b => b.RunAsync()).ThrowsAsync(new OperationCanceledException());

		Worker worker = new(_serviceProviderMock.Object, _loggerMock!.Object, _publicIpMock.Object);

		// Act
		await worker.StartAsync(CancellationToken.None);

		// Assert
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		_loggerMock.Verify(l => l.Log(
		  LogLevel.Information,
		  It.IsAny<EventId>(),
		  It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("NLBE Bot was cancelled gracefully.")),
		  It.IsAny<OperationCanceledException>(),
		  It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CS8602
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenServiceProviderIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Worker(null, _loggerMock!.Object, _publicIpMock!.Object));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Worker(_serviceProviderMock!.Object, null, _publicIpMock!.Object));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenPublicIpAddressIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Worker(_serviceProviderMock!.Object, _loggerMock!.Object, null));
	}
}
