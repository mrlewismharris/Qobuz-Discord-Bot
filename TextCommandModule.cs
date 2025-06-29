using CliWrap;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Services.Commands;
using System.Diagnostics;
using System.Text;

namespace QobuzDiscordBot;

public class TextCommandModule : CommandModule<CommandContext>
{
    private readonly DataContext _dbContext;
    private readonly string _rootPath;

    public TextCommandModule(DataContext dbContext)
    {
        _dbContext = dbContext;
        _rootPath = Directory.GetCurrentDirectory();
    }

    [Command("ping")]
    public string Ping() {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} ---> User: {Context.User.Email} did a ping.");
        return "Pong!";
    }

    [Command(["h", "help", "commands", "info"])]
    public static string Commands() => """
        Qobuz Discord Music Bot Commands:

        !h, !help, !commands !info ---> Get bot information.
        !s, !status --> Get current bot status.
        !p *query*, !play *query* --> Play a query (track, e.g. !p hey jude).
        !skip --> Skips currently playing song.
        !kick --> Kick the bot from the voice channel.
        """;

    [Command(["p", "play"])]
    public async Task<string> Play([CommandParameter(Remainder = true)] string query)
    {
        var guild = Context.Guild!;
        if (!guild.VoiceStates.TryGetValue(Context.User.Id, out var voiceState))
            return "You are not connected to a voice channel.";

        var client = Context.Client;

        //todo: should check here if bot is already in channel

        var voiceClient = await client.JoinVoiceChannelAsync(
            guild.Id,
            voiceState.ChannelId.GetValueOrDefault(),
            new VoiceClientConfiguration
            {
                Logger = new ConsoleLogger()
            });

        await voiceClient.StartAsync();

        await voiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone));

        await Context.Message.ReplyAsync($"Downloading song \"{query}\"...");

        var dlResult = await DownloadTrack(query);

        //todo: error checking

        var trackPath = GetTrackPathById(dlResult.TrackId);

        if (string.IsNullOrWhiteSpace(trackPath))
            return "Failed to download and play track.";

        await Context.Message.ReplyAsync($"Found and downloaded song: \"{Path.GetFileNameWithoutExtension(trackPath).Replace(dlResult.TrackId, "")}\" - if this is not correct, use !playr *query* (coming soon).");
        var outStream = voiceClient.CreateOutputStream();

        OpusEncodeStream stream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);
        ProcessStartInfo startInfo = new("ffmpeg")
        {
            RedirectStandardOutput = true
        };
        var arguments = startInfo.ArgumentList;
        // arguments.Add("-reconnect");
        // arguments.Add("1");
        // arguments.Add("-reconnect_streamed");
        // arguments.Add("1");
        // arguments.Add("-reconnect_delay_max");
        // arguments.Add("5");
        arguments.Add("-i");
        arguments.Add(trackPath);
        // arguments.Add("-loglevel");
        // arguments.Add("-8");
        arguments.Add("-ac");
        arguments.Add("2");
        arguments.Add("-f");
        arguments.Add("s16le");
        arguments.Add("-ar");
        arguments.Add("48000");
        arguments.Add("pipe:1");
        var ffmpeg = Process.Start(startInfo);
        if (ffmpeg == null)
            return "Failed to start ffmpeg.";
        await Context.Message.ReplyAsync($"Playing: \"{Path.GetFileNameWithoutExtension(trackPath).Replace(dlResult.TrackId, "")}\".");
        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream);
        await stream.FlushAsync();
        return "";
    }

    [Command(["s", "status"])]
    public static string Status() => "Not yet implemented";

    [Command(["skip"])]
    public static string Skip() => "Not yet implemented";

    [Command(["k", "kick", "q", "quit", "leave"])]
    public async Task Kick()
    {
        var client = Context.Client;
        var guild = Context.Guild;

        // Get the current user (bot)
        var botUser = await client.Rest.GetCurrentUserAsync();

        // Check if the bot is in a voice channel
        if (!guild.VoiceStates.TryGetValue(botUser.Id, out var voiceState) || !voiceState.ChannelId.HasValue)
            await Context.Channel.SendMessageAsync("I'm not connected to any voice channel!");

        // Leave the voice channel
        await client.UpdateVoiceStateAsync(new VoiceStateProperties(guild.Id, null));
        await Context.Channel.SendMessageAsync("Left the voice channel!");
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
}


