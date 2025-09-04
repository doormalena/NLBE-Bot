namespace NLBE_Bot.Models;

using DSharpPlus.Entities;
using NLBE_Bot.Interfaces;
using System.Collections.Generic;

internal class EmbedOptions
{
	public string Thumbnail
	{
		get; set;
	} = string.Empty;

	public string Content
	{
		get; set;
	} = string.Empty;

	public string Title
	{
		get; set;
	} = string.Empty;

	public string Description
	{
		get; set;
	} = string.Empty;

	public string Footer
	{
		get; set;
	} = string.Empty;

	public List<DEF> Fields
	{
		get; set;
	} = [];

	public List<IDiscordEmoji> Emojis
	{
		get; set;
	} = [];

	public string ImageUrl
	{
		get; set;
	} = string.Empty;

	public DiscordEmbedBuilder.EmbedAuthor Author
	{
		get; set;
	} = new();

	public DiscordColor Color { get; set; } = Constants.BOT_COLOR;

	public bool IsForReplay
	{
		get; set;
	}

	public string NextMessage
	{
		get; set;
	} = string.Empty;
}
