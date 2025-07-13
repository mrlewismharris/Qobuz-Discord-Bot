using Microsoft.EntityFrameworkCore;
using NetCord.Gateway.Voice;
using NetCord.Services.Commands;
using QobuzDiscordBot.Models.DbModels;
using QobuzDiscordBot.Models.Dtos;
using System.Diagnostics;

namespace QobuzDiscordBot.Services;

public class PlaybackService
{
    public readonly DataContext _context;
    public readonly IOService _ioService;
    private VoiceClientService _voiceClientService;
    private Process? _ffmpegProcess;
    private DownloadedTrack? _nowPlaying;

    public PlaybackService(DataContext context, IOService ioService, VoiceClientService voiceClientService)
    {
        _context = context;
        _ioService = ioService;
        _voiceClientService = voiceClientService;
        _nowPlaying = null;
    }

    public async void Add(DownloadedTrack track, CommandContext? context)
    {
        SongQueue QueueItem = new SongQueue
        {
            Id = track.Id,
            Filename = track.Filename,
            TimeStamp = DateTime.Now
        };
        await _context.SongQueue.AddAsync(QueueItem);
        await _context.SaveChangesAsync();
        RecheckPlaybackQueue(context);
    }

    public async void RecheckPlaybackQueue(CommandContext? context)
    {
        if (_ffmpegProcess != null || !await _context.SongQueue.AnyAsync())
            return;

        SongQueue nextInQueue = await _context.SongQueue
            .OrderBy(q => q.TimeStamp)
            .FirstAsync();

        if (!await _context.DownloadedTracks.AnyAsync(dt => dt.Filename == nextInQueue.Filename))
            return;

        _context.SongQueue.Remove(nextInQueue);
        await _context.SaveChangesAsync();

        Play(nextInQueue.Id, context);
    }

    public async Task Play(int id, CommandContext? context)
    {
        var dbTrack = await _context.DownloadedTracks.Where(t => t.Id == id).FirstOrDefaultAsync();

        _nowPlaying = dbTrack;

        if (dbTrack == null)
            return;

        if (!_ioService.TrackExists(dbTrack.Filename))
            return;

        context.Message.SendAsync($"Now Playing: {_nowPlaying.Performer} - {_nowPlaying.Title}");

        await _voiceClientService.Client.StartAsync();
        await _voiceClientService.Client.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone));
        var outStream = _voiceClientService.Client.CreateOutputStream();
        OpusEncodeStream stream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);
        ProcessStartInfo startInfo = new("ffmpeg")
        {
            RedirectStandardOutput = true
        };
        var arguments = startInfo.ArgumentList;
        arguments.Add("-loglevel");
        arguments.Add("error");
        arguments.Add("-i");
        arguments.Add(_ioService.GetFullPathFromFileName(dbTrack.Filename));
        arguments.Add("-ac");
        arguments.Add("2");
        arguments.Add("-f");
        arguments.Add("s16le");
        arguments.Add("-ar");
        arguments.Add("48000");
        arguments.Add("pipe:1");
        _ffmpegProcess = Process.Start(startInfo);
        if (_ffmpegProcess == null)
            return;
        await _ffmpegProcess.StandardOutput.BaseStream.CopyToAsync(stream);
        await stream.FlushAsync();
        await _ffmpegProcess.WaitForExitAsync();
        _ffmpegProcess = null;
        await _voiceClientService.Client.CloseAsync();
        _nowPlaying = null;
        RecheckPlaybackQueue(context);
    }

    public async Task<bool> Skip(CommandContext? context)
    {
        if (_ffmpegProcess == null)
            return false;
        else
        {
            await _voiceClientService.Client.CloseAsync();
            _ffmpegProcess.Close();
            _ffmpegProcess = null;
            _nowPlaying = null;
            RecheckPlaybackQueue(context);
            return true;
        }
    }

    public async Task<int> CountSongsInQueue() =>
        await _context.SongQueue.CountAsync();

    public async Task<IEnumerable<SongQueue>> GetQueuedSongs() =>
        await _context.SongQueue.Include(sq => sq.DownloadedTrack).ToListAsync();

    public async Task ClearSongQueue(CommandContext? context)
    {
        _context.SongQueue.RemoveRange(_context.SongQueue.ToList());
        await _context.SaveChangesAsync();
        await Skip(context);
    }

    public string GetNowPlaying() => _nowPlaying == null ? $"Nothing currently playing." : $"Now Playing: {_nowPlaying.Performer} - {_nowPlaying.Title}.";

    public async Task<IEnumerable<SongQueue>> GetQueue() => await _context.SongQueue.OrderBy(s => s.TimeStamp).Include(s => s.DownloadedTrack).ToListAsync();
}

