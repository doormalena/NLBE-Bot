namespace NLBE_Bot.Services;

using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal class BotState(string stateFile = "botstate.json", bool autoSave = true) : IBotState
{
	private readonly string _stateFile = stateFile ?? throw new ArgumentNullException(nameof(stateFile));
	private readonly bool _autoSave = autoSave;
	private readonly Lock _lock = new();
	private BotStateData _data = new();

	public bool IgnoreCommands
	{
		get
		{
			lock (_lock)
			{
				return _data.IgnoreCommands;
			}
		}
		set
		{
			lock (_lock)
			{
				_data.IgnoreCommands = value;
				if (_autoSave)
				{
					Task.Run(() => SaveAsync());
				}
			}
		}
	}

	public bool IgnoreEvents
	{
		get
		{
			lock (_lock)
			{
				return _data.IgnoreEvents;
			}
		}
		set
		{
			lock (_lock)
			{
				_data.IgnoreEvents = value;
				if (_autoSave)
				{
					Task.Run(() => SaveAsync());
				}
			}
		}
	}


	public WeeklyEventWinner? WeeklyEventWinner
	{
		get
		{
			lock (_lock)
			{
				return _data.WeeklyEventWinner;
			}
		}
		set
		{
			lock (_lock)
			{
				_data.WeeklyEventWinner = value;
				if (_autoSave)
				{
					Task.Run(() => SaveAsync());
				}
			}
		}
	}

	private IDiscordMessage? _lastCreatedDiscordMessage;

	public IDiscordMessage? LastCreatedDiscordMessage
	{
		get
		{
			lock (_lock)
			{
				return _lastCreatedDiscordMessage;
			}
		}
		set
		{
			lock (_lock)
			{
				_lastCreatedDiscordMessage = value;
			}
		}
	}

	public DateTime? LasTimeServerNicknamesWereVerified
	{
		get
		{
			lock (_lock)
			{
				return _data.LasTimeServerNicknamesWereVerified;
			}
		}
		set
		{
			lock (_lock)
			{
				_data.LasTimeServerNicknamesWereVerified = value;
				if (_autoSave)
				{
					Task.Run(() => SaveAsync());
				}
			}
		}
	}

	public DateTime? LastWeeklyWinnerAnnouncement
	{
		get
		{
			lock (_lock)
			{
				return _data.LastWeeklyWinnerAnnouncement;
			}
		}
		set
		{
			lock (_lock)
			{
				_data.LastWeeklyWinnerAnnouncement = value;
				if (_autoSave)
				{
					Task.Run(() => SaveAsync());
				}
			}
		}
	}

	public async Task SaveAsync()
	{
		string json = JsonSerializer.Serialize(_data);
		await File.WriteAllTextAsync(_stateFile, json);
	}

	public async Task LoadAsync()
	{
		if (!File.Exists(_stateFile))
		{
			return;
		}

		using FileStream stream = new(_stateFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using StreamReader reader = new(stream);
		string json = await reader.ReadToEndAsync();

		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}

		BotStateData? data = JsonSerializer.Deserialize<BotStateData>(json);

		if (data != null)
		{
			_data = data;
		}
	}
}
