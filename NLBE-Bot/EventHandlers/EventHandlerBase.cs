namespace NLBE_Bot.EventHandlers;

using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using System;
using System.Threading.Tasks;

internal abstract class EventHandlerBase(IOptions<BotOptions> options)
{
	protected readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	protected IBotState? _botState;

	public abstract void Register(IDiscordClient client, IBotState botState);

	protected async Task ExecuteIfAllowedAsync(IDiscordGuild guild, Func<Task> action)
	{
		if (_botState!.IgnoreEvents || guild.Id != _options.ServerId)
		{
			return;
		}

		await action();
	}
}
