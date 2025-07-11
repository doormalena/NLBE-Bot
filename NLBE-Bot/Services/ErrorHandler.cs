namespace NLBE_Bot.Services;

using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Threading.Tasks;

public class ErrorHandler(ILogger<ErrorHandler> logger) : IErrorHandler
{
	private readonly ILogger<ErrorHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async Task HandleErrorAsync(string message, Exception ex = null)
	{
		_logger.LogError(ex, "{Message}{ExMessage}{NewLine}{ExStackTrace}", message, ex?.Message, Environment.NewLine, ex?.StackTrace);

		// Optionally: send to Discord channel.
		await Task.CompletedTask;
	}
}
