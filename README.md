# NLBE-Bot

## Installation

### On Windows

01. Copy the binaries to `C:\ProgramData\NLBEBot\`
02. Install the latest .NET 9.0 Runtime from <https://dotnet.microsoft.com/en-us/download/dotnet/9.0>.	
03. Run PowerShell as an Administrator.
04. Register a Windows Service by executing the following commands:

	```pwsh
	sc create "NLBE-Bot" binpath="C:\ProgramData\NLBEBot\NLBE-Bot.exe"
	sc config "NLBE-Bot" start=auto
	```

### Configuration

  ``NLBEBOT:DiscordToken`` --> create new app on https://discord.com/developers/applications using [Discord Application Setup Guide](docs/discord-application-setup-guide.md)
  ``NLBEBOT:WarGamingAppId`` --> Create new app of type `Server` on https://developers.wargaming.net/applications/. Ensure all IP addresses of the running server are registered. Copy the ID into the configuration.

