namespace NLBE_Bot.Models;

using DSharpPlus.Entities;
using NLBE_Bot.Interfaces;
using System.Collections.Generic;

internal class EmbedOptions
{
	public string Thumbnail
	{
		get; set;
	}
	public string Content
	{
		get; set;
	}
	public string Title
	{
		get; set;
	}
	public string Description
	{
		get; set;
	}
	public string Footer
	{
		get; set;
	}
	public List<DEF> Fields
	{
		get; set;
	}
	public List<IDiscordEmoji> Emojis
	{
		get; set;
	}
	public string ImageUrl
	{
		get; set;
	}
	public DiscordEmbedBuilder.EmbedAuthor Author
	{
		get; set;
	}
	public DiscordColor Color { get; set; } = Constants.BOT_COLOR;
	public bool IsForReplay
	{
		get; set;
	}
	public string NextMessage
	{
		get; set;
	}
}
