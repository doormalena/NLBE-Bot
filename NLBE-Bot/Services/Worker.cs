namespace NLBE_Bot.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class Worker(IServiceProvider serviceProvider, ILogger<Worker> logger, IPublicIpAddress publicIpAddress) : BackgroundService
{
	private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	private readonly ILogger<Worker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IPublicIpAddress _publicIpAddress = publicIpAddress ?? throw new ArgumentNullException(nameof(publicIpAddress));

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("NLBE Bot is starting.");

		string ipAddress = await _publicIpAddress.GetPublicIpAddressAsync();
		_logger.LogInformation("Ensure the public ip address {IpAddress} is allowed to access the WarGaming application.", ipAddress);

		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				IBot bot = _serviceProvider.GetRequiredService<IBot>();
				await bot.RunAsync(); // Note: the bot does not yet support gracefull cancellation.
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
