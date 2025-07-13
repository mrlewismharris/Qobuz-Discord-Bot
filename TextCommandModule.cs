using CliWrap;
using Microsoft.Extensions.Configuration;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Services.Commands;
using QobuzApiSharp.Service;
using QobuzDiscordBot.Models.Dtos;
using QobuzDiscordBot.Models.ViewModels;
using QobuzDiscordBot.Services;
using System.Diagnostics;
using System.Text;

namespace QobuzDiscordBot;

public class TextCommandModule : CommandModule<CommandContext>
{
    private readonly DataContext _dbContext;
    private readonly string _rootPath;
    private readonly QobuzApiService _qobuz;
    private readonly SearchCacheService _searchCache;
    private readonly string _prefix;
    private readonly IOService _ioService;
    private readonly DownloadService _downloadService;
    private readonly VoiceClientService _voiceClientService;
    private readonly PlaybackService _playbackService;

    public TextCommandModule(DataContext dbContext, QobuzApiService qobuz, IConfiguration config, SearchCacheService searchCache, IOService ioService, DownloadService downloadService, VoiceClientService voiceClientService, PlaybackService playbackService)
    {
        _dbContext = dbContext;
        _rootPath = Directory.GetCurrentDirectory();
        _qobuz = qobuz;
        _searchCache = searchCache;
        _prefix = config["DISCORD_PREFIX"] ?? "!";
        _ioService = ioService;
        _downloadService = downloadService;
        _voiceClientService = voiceClientService;
        _playbackService = playbackService;
    }

    [Command("ping")]
    public string Ping()
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} ---> User: {Context.User.Email} did a ping.");
        return "Pong!";
    }

    [Command(["h", "help", "commands", "info"])]
    public string Commands() => $"""
        Qobuz Discord Music Bot Commands:

        {_prefix}play *query*, {_prefix}p *query* --> Plays first search result (e.g. {_prefix}p hey jude).
        {_prefix}search, {_prefix}s *query* ---> Search for a track.
        {_prefix}sel *number* ---> Select a track from the search list ,or load more.
        {_prefix}statuc, {_prefix}status --> Get current bot status.
        {_prefix}skip --> Skips currently playing song.
        {_prefix}kick --> Kick the bot from the voice channel.
        {_prefix}h, {_prefix}help, {_prefix}commands ---> Get bot information.
        {_prefix}info ---> Get current bot info.
        """;

    [Command(["p", "play"])]
    public async Task Play([CommandParameter(Remainder = true)] string query)
    {
        var guild = Context.Guild!;
        if (!guild.VoiceStates.TryGetValue(Context.User.Id, out var voiceState))
        {
            await Context.Message.ReplyAsync("You are not connected to a voice channel.");
            return;
        }

        var client = Context.Client;

        if (query == null || string.IsNullOrWhiteSpace(query) || query.Length < 4)
        {
            await Context.Message.ReplyAsync("Query is required and must be more than 3 characters.");
            return;
        }

        if (_voiceClientService.Client == null)
            _voiceClientService.Client = await client.JoinVoiceChannelAsync(
                guild.Id,
                voiceState.ChannelId.GetValueOrDefault(),
                new VoiceClientConfiguration
                {
                    Logger = new ConsoleLogger()
                });

        var track = (_qobuz.SearchTracks(query, 1)).Tracks?.Items?.FirstOrDefault();

        if (track == null)
        {
            await Context.Message.ReplyAsync($"No tracks found with the query: \"{query}\"");
            return;
        }

        _downloadService.Add(new TrackDto
        {
            Id = track.Id,
            Title = track.Title,
            Duration = track.Duration,
            Performer = track.Performer.Name,
            Version = track.Version
        }, new DiscordUser
        {
            Id = Context.User.Id,
            Email = Context.User.Email,
            Username = Context.User.Username,
            GlobalName = Context.User.GlobalName
        }, Context);

        await Context.Message.ReplyAsync($"Found track {track.Title} - {track.Performer.Name}");
    }

    [Command(["s", "search"])]
    public async Task<string> Search([CommandParameter(Remainder = true)] string query)
    {
        var guild = Context.Guild!;
        if (!guild.VoiceStates.TryGetValue(Context.User.Id, out var voiceState))
            return "You are not connected to a voice channel.";

        var client = Context.Client;

        if (query == null || string.IsNullOrWhiteSpace(query) || query.Length < 4)
            return "Query is required and must be more than 3 characters.";

        if (_voiceClientService.Client == null)
            _voiceClientService.Client = await client.JoinVoiceChannelAsync(
                guild.Id,
                voiceState.ChannelId.GetValueOrDefault(),
                new VoiceClientConfiguration
                {
                    Logger = new ConsoleLogger()
                });

        Stopwatch timer = Stopwatch.StartNew();
        var userId = Context.User.Id;
        await Context.Message.ReplyAsync($"Searching for query \"{query}\"...");
        if (_searchCache.Cache.Any(s => s.UserId == userId))
            _searchCache.Cache.Remove(_searchCache.Cache.First(s => s.UserId == userId));
        var results = _qobuz.SearchTracks(query, 5);

        if (!results.Tracks.Items.Any())
            return $"No tracks found with the query: \"{query}\"";

        var userSearch = new UserSearch
        {
            UserId = userId,
            SearchQuery = query,
            Results = results.Tracks.Items.Select(t => new TrackDto
            {
                Id = t.Id,
                Title = t.Title,
                Duration = t.Duration,
                Performer = t.Performer.Name,
                Version = t.Version
            }),
            Offset = 0,
            DateTime = DateTime.Now

        };
        _searchCache.Cache.Add(userSearch);
        timer.Stop();

        return $"""
            Found 5 results in {timer.ElapsedMilliseconds}ms w/ no offset:

            {string.Join("\n", userSearch.Results.Select((r, i) => $"{i}. {r.DisplayInfo})"))}
            6. Search for more...
        
            Use {_prefix}sel *number*
        """;
    }

    [Command(["sel", "select", "l"])]
    public async Task SelectSearch([CommandParameter(Remainder = true)] string selectedTrackString)
    {
        var userId = Context.User.Id;
        if (!_searchCache.Cache.Any(s => s.UserId == userId))
        {
            await Context.Message.SendAsync("You have no searches. Use !search or !s first (e.g. !s hey jude), then select from the list.");
            return;
        }

        if (selectedTrackString.Length > 1)
        {
            await Context.Message.SendAsync("!sel only accepts 1 character");
            return;
        }

        if (!int.TryParse(selectedTrackString, out int selectedTrack))
        {
            await Context.Message.SendAsync($"Track selection must be a number");
            return;
        }

        if (selectedTrack < 1 || selectedTrack > 6)
        {
            await Context.Message.SendAsync($"Track selection must be a number between 1-6");
            return;
        }

        var userSearch = _searchCache.Cache.First(s => s.UserId == userId);

        if (userSearch.DateTime < DateTime.Now.AddMinutes(-2)) //maybe do custom request timeout here from .env?
        {
            _searchCache.Cache.Remove(userSearch);
            await Context.Message.SendAsync($"Search selection timed out. Search again and select a track within 2 minutes.");
            return;
        }

        if (selectedTrack == 6)
        {
            Stopwatch timer = Stopwatch.StartNew();
            var newOffset = userSearch.Offset + 5;
            await Context.Message.ReplyAsync($"Searching for query \"{userSearch.SearchQuery}\" (skipping offset {newOffset})...");
            if (_searchCache.Cache.Any(s => s.UserId == userId))
                _searchCache.Cache.Remove(_searchCache.Cache.First(s => s.UserId == userId));
            var results = _qobuz.SearchTracks(userSearch.SearchQuery, 5, newOffset);

            if (!results.Tracks.Items.Any())
            {
                await Context.Message.SendAsync($"No tracks found with the query: \"{userSearch.SearchQuery}\"");
                return;
            }

            userSearch = new UserSearch
            {
                UserId = userId,
                SearchQuery = userSearch.SearchQuery,
                Results = results.Tracks.Items.Select(t => new TrackDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Duration = t.Duration,
                    Performer = t.Performer.Name,
                    Version = t.Version
                }),
                Offset = newOffset,
                DateTime = DateTime.Now

            };
            _searchCache.Cache.Add(userSearch);
            timer.Stop();

            await Context.Message.SendAsync($"""
                Found {userSearch.Results.Count()} results in {timer.ElapsedMilliseconds}ms w/ offset {newOffset}:

                {string.Join("\n", userSearch.Results.Select((r, i) => $"{i}. {r.DisplayInfo}"))}
                6. Search for more...

                Use {_prefix}sel *number*
            """);
        }

        var track = userSearch.Results.ElementAtOrDefault(selectedTrack - 1);
        if (track == null)
        {
            Context.Message.SendAsync($"There was no song at the index {selectedTrack}. Please try using {_prefix}sel again.");
            return;
        }
        _searchCache.Cache.Remove(_searchCache.Cache.First(s => s.UserId == userId));
        _downloadService.Add(track, new DiscordUser
        {
            Id = Context.User.Id,
            Email = Context.User.Email,
            Username = Context.User.Username,
            GlobalName = Context.User.GlobalName
        });
    }

    [Command(["status"])]
    public static string Status() => "Not yet implemented";

    [Command(["skip"])]
    public async Task<string> Skip()
    {
        if (await _playbackService.Skip())
            return "Skipped";
        return "Not yet implemented";
    }

    [Command(["k", "kick", "q", "quit", "leave"])]
    public async Task Kick()
    {
        var client = Context.Client;
        var guild = Context.Guild;

        // Get the current user (bot)
        var botUser = await client.Rest.GetCurrentUserAsync();

        // Check if the bot is in a voice channel
        if (!guild!.VoiceStates.TryGetValue(botUser.Id, out var voiceState) || !voiceState.ChannelId.HasValue)
            await Context.Channel!.SendMessageAsync("I'm not connected to any voice channel!");

        // Leave the voice channel
        await client.UpdateVoiceStateAsync(new VoiceStateProperties(guild.Id, null));
        await Context.Channel!.SendMessageAsync("Left the voice channel!");
    }

    public static async Task<(bool Success, string TrackId, List<string> Errors, string Output)> DownloadTrack(string query)
    {
        var stdErrBuffer = new StringBuilder();
        Random random = new Random();
        var newId = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 8).Select(s => s[random.Next(s.Length)]).ToArray()).ToLower();

        var result = await Cli.Wrap("qdl")
            .WithArguments([
                "lucky",
                "-t", "track",
                "--d", "./Music",
                "--no-cover",
                "--no-db",
                "--no-m3u",
                "-q", "5",
                "-ff", ".",
                "-tf", $"{"{tracktitle}"} {newId}",
                query
            ])
            .WithValidation(CommandResultValidation.None)
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .ExecuteAsync();

        return (true, newId, new List<string>(), stdErrBuffer.ToString());
    }

    public string? GetTrackPathById(string fileId) =>
        Directory.EnumerateFiles(Path.Combine(_rootPath, "Music"), "*", SearchOption.AllDirectories).FirstOrDefault(f => Path.GetFileName(f).Contains(fileId)) ?? null;

    [Command("test")]
    public string Test()
    {
        return _ioService.GetStorageLimit().ToString();
    }
}


