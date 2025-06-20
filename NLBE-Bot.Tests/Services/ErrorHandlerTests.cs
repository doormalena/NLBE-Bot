namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using NLBE_Bot.Services;
using NSubstitute;
using System;
using System.Threading.Tasks;

[TestClass]
public class ErrorHandlerTests
{
	[TestMethod]
	public async Task HandleErrorAsync_LogsError_WithMessageOnly()
	{
		// Arrange.
		ILogger<ErrorHandler> loggerMock = Substitute.For<ILogger<ErrorHandler>>();
		ErrorHandler handler = new(loggerMock);
		string message = "Test error message";

		// Act.
		await handler.HandleErrorAsync(message);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		loggerMock.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v != null && v.ToString().Contains(message)),
			null,
			Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CS8602
	}

	[TestMethod]
	public async Task HandleErrorAsync_LogsError_WithException()
	{
		// Arrange.
		ILogger<ErrorHandler> loggerMock = Substitute.For<ILogger<ErrorHandler>>();
		ErrorHandler handler = new(loggerMock);
		string message = "Test error message";
		InvalidOperationException exception = new("Test exception");

		// Act.
		await handler.HandleErrorAsync(message, exception);

		// Assert.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		loggerMock.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v != null &&
							v.ToString().Contains(message) &&
							v.ToString().Contains(exception.Message)),
			exception,
			Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CS8602
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new ErrorHandler(null));
	}
}
