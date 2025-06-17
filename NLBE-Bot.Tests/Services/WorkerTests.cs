namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Moq;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using System;

[TestClass]
public class WorkerTests
{
	[TestMethod]
	public async Task ExecuteAsync_LogsStartupAndShutdown()
	{
		// Arrange
		Mock<ILogger<Worker>> loggerMock = new();
		Mock<IServiceProvider> serviceProviderMock = new();
		Mock<IPublicIpAddress> publicIpMock = new();
		Mock<IBot> botMock = new();

		serviceProviderMock.Setup(sp => sp.GetService(typeof(IBot))).Returns(botMock.Object);
		publicIpMock.Setup(m => m.GetPublicIpAddressAsync()).ReturnsAsync("1.2.3.4");
		botMock.Setup(b => b.RunAsync()).Returns(Task.CompletedTask);

		Worker worker = new(serviceProviderMock.Object, loggerMock.Object, publicIpMock.Object);

		using CancellationTokenSource cts = new();
		cts.CancelAfter(10); // Cancel quickly to exit loop

		// Act.
		await worker.StartAsync(cts.Token);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		loggerMock.Verify(l => l.Log(
		  LogLevel.Information,
		  It.IsAny<EventId>(),
		  It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("NLBE Bot is starting.")),
		  null,
		  It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

		loggerMock.Verify(l => l.Log(
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
		Mock<ILogger<Worker>> loggerMock = new();
		Mock<IServiceProvider> serviceProviderMock = new();
		Mock<IPublicIpAddress> publicIpMock = new();
		Mock<IBot> botMock = new();

		serviceProviderMock.Setup(sp => sp.GetService(typeof(IBot))).Returns(botMock.Object);
		publicIpMock.Setup(m => m.GetPublicIpAddressAsync()).ReturnsAsync("1.2.3.4");
		botMock.Setup(b => b.RunAsync()).ThrowsAsync(new OperationCanceledException());


		Worker worker = new(serviceProviderMock.Object, loggerMock.Object, publicIpMock.Object);

		// Act
		await worker.StartAsync(CancellationToken.None);

		// Assert
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		loggerMock.Verify(l => l.Log(
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
		// Act.		
		Mock<ILogger<Worker>> loggerMock = new();
		Mock<IPublicIpAddress> publicIpMock = new();

		// Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Worker(null, loggerMock.Object, publicIpMock.Object));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
	{
		// Act.		
		Mock<IServiceProvider> serviceProviderMock = new();
		Mock<IPublicIpAddress> publicIpMock = new();

		// Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Worker(serviceProviderMock.Object, null, publicIpMock.Object));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenPublicIpAddressIsNull()
	{
		// Act.
		Mock<IServiceProvider> serviceProviderMock = new();
		Mock<ILogger<Worker>> loggerMock = new();

		// Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new Worker(serviceProviderMock.Object, loggerMock.Object, null));
	}
}
