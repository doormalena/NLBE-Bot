namespace NLBE_Bot;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("NLBE Bot is starting.");

		string ipAddress = await PublicIpAddress.GetPublicIpAddressAsync();
		_logger.LogInformation("Ensure the public ip address {IpAddress} is allowed to access the WarGaming application.", ipAddress);

		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await new Bot(_logger, _configuration).RunAsync(); // Note: the bot does not yet support gracefull cancellation.
			}
		}
		catch (OperationCanceledException ex)
		{
			_logger.LogInformation(ex, "NLBE Bot was cancelled gracefully.");
		}
		finally
		{
			_logger.LogInformation("NLBE Bot is stopped.");
		}
	}
}
