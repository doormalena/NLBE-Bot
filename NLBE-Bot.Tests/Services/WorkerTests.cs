namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;

[TestClass]
public class WorkerTests
{
	[TestMethod]
	public async Task ExecuteAsync_LogsStartupAndShutdown()
	{
		// Arrange
		Mock<ILogger<Worker>> loggerMock = new();
		Mock<IServiceProvider> serviceProviderMock = new();
		Mock<IBot> botMock = new();
		botMock.Setup(b => b.RunAsync()).Returns(Task.CompletedTask);

		serviceProviderMock.Setup(sp => sp.GetService(typeof(IBot))).Returns(botMock.Object);

		Mock<IPublicIpAddress> publicIpMock = new();
		publicIpMock.Setup(m => m.GetPublicIpAddressAsync()).ReturnsAsync("1.2.3.4");

		Worker worker = new(serviceProviderMock.Object, loggerMock.Object, publicIpMock.Object);

		using CancellationTokenSource cts = new();
		cts.CancelAfter(10); // Cancel quickly to exit loop

		// Act.
		await worker.StartAsync(cts.Token);

		// Assert.
		loggerMock.Verify(l => l.Log(
		  LogLevel.Information,
		  It.IsAny<EventId>(),
		  It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("NLBE Bot is starting.")),
		  null,
		  It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

		loggerMock.Verify(l => l.Log(
		  LogLevel.Information,
		  It.IsAny<EventId>(),
		  It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("NLBE Bot is stopped.")),
		  null,
		  It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
	}

	[TestMethod]
	public async Task ExecuteAsync_HandlesOperationCanceledException()
	{
		// Arrange
		Mock<ILogger<Worker>> loggerMock = new();
		Mock<IServiceProvider> serviceProviderMock = new();
		Mock<IBot> botMock = new();
		botMock.Setup(b => b.RunAsync()).ThrowsAsync(new OperationCanceledException());

		serviceProviderMock.Setup(sp => sp.GetService(typeof(IBot))).Returns(botMock.Object);

		Mock<IPublicIpAddress> publicIpMock = new();
		publicIpMock.Setup(m => m.GetPublicIpAddressAsync()).ReturnsAsync("1.2.3.4");

		Worker worker = new(serviceProviderMock.Object, loggerMock.Object, publicIpMock.Object);

		// Act
		await worker.StartAsync(CancellationToken.None);

		// Assert
		loggerMock.Verify(l => l.Log(
		  LogLevel.Information,
		  It.IsAny<EventId>(),
		  It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("NLBE Bot was cancelled gracefully.")),
		  It.IsAny<OperationCanceledException>(),
		  It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
	}
}
