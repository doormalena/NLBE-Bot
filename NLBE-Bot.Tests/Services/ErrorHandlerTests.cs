namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Moq;
using NLBE_Bot.Services;
using System;
using System.Threading.Tasks;

[TestClass]
public class ErrorHandlerTests
{
	[TestMethod]
	public async Task HandleErrorAsync_LogsError_WithMessageOnly()
	{
		// Arrange.
		Mock<ILogger<ErrorHandler>> loggerMock = new();
		ErrorHandler handler = new(loggerMock.Object);
		string message = "Test error message";

		// Act.
		await handler.HandleErrorAsync(message);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		loggerMock.Verify(l => l.Log(
			LogLevel.Error,
			It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains(message)),
			null,
			It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
		Times.Once);
#pragma warning restore CS8602
	}

	[TestMethod]
	public async Task HandleErrorAsync_LogsError_WithException()
	{
		// Arrange.
		Mock<ILogger<ErrorHandler>> loggerMock = new();
		ErrorHandler handler = new(loggerMock.Object);
		string message = "Test error message";
		InvalidOperationException exception = new("Test exception");

		// Act.
		await handler.HandleErrorAsync(message, exception);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		loggerMock.Verify(l => l.Log(
			LogLevel.Error,
			It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((v, t) =>
				v != null &&
				v.ToString().Contains(message) &&
				v.ToString().Contains(exception.Message)),
			exception,
			It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
		Times.Once);
#pragma warning restore CS8602
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new ErrorHandler(null));
	}
}
