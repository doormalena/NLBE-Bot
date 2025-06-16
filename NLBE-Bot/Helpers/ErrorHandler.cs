namespace NLBE_Bot.Helpers;

using DSharpPlus;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Threading.Tasks;

public class ErrorHandler(ILogger<ErrorHandler> logger) : IErrorHandler
{
	private readonly ILogger<ErrorHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async Task HandleErrorAsync(string message, Exception ex = null)
	{
		string formattedMessage = $"{message}{ex?.Message}{Environment.NewLine}{ex?.StackTrace}";
		_logger.LogError(ex, formattedMessage);

		// Optionally: send to Discord channel.
		await Task.CompletedTask;
	}
}
