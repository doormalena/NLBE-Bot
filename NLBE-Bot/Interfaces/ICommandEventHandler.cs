namespace NLBE_Bot.Interfaces;

using DSharpPlus.CommandsNext;

internal interface ICommandEventHandler
{
	public void Register(ICommandsNextExtension commands);
}
