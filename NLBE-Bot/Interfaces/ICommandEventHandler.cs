namespace NLBE_Bot.Interfaces;

internal interface ICommandEventHandler
{
	public void Register(ICommandsNextExtension commands);
}
