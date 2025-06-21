namespace NLBE_Bot.Interfaces;

internal interface IMessageEventHandler
{
	public void Register(IDiscordClient client);
}
