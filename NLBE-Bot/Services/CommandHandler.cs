namespace NLBE_Bot.Services;

using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Threading.Tasks;

internal class CommandHandler(ILogger<CommandHandler> logger, IErrorHandler errorHandler, IDiscordMessageUtils discordMessageUtils) : ICommandHandler
{
	private readonly ILogger<CommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));

	public Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
	{
		_logger.LogInformation("Command executed: {CommandName}", e.Command.Name);

		return Task.CompletedTask;
	}

	public Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
	{
		if (!e.Context.Guild.Id.Equals(Constants.NLBE_SERVER_ID) && !e.Context.Guild.Id.Equals(Constants.DA_BOIS_ID))
		{
			return Task.CompletedTask;
		}

		if (e.Exception.Message.Contains("unauthorized", StringComparison.CurrentCultureIgnoreCase))
		{
			e.Context.Channel.SendMessageAsync("**De bot heeft hier geen rechten voor!**");
		}
		else if (e.Command != null)
		{
			e.Context.Message.DeleteReactionsEmojiAsync(_discordMessageUtils.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION));
			e.Context.Message.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(Constants.ERROR_REACTION));
			_errorHandler.HandleErrorAsync("Error with command (" + e.Command.Name + "):\n", e.Exception).Wait();
		}

		return Task.CompletedTask;
	}
}
