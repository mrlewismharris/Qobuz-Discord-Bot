using Microsoft.EntityFrameworkCore;
using NetCord.Gateway.Voice;
using QobuzDiscordBot.Models.DbModels;
using System.Diagnostics;

namespace QobuzDiscordBot.Services;

public class PlaybackService
{
    public readonly DataContext _context;
    public readonly IOService _ioService;
    private VoiceClientService _voiceClientService;
    private Process? _ffmpegProcess;

    public PlaybackService(DataContext context, IOService ioService, VoiceClientService voiceClientService)
    {
        _context = context;
        _ioService = ioService;
        _voiceClientService = voiceClientService;
    }

    public async void Add(DownloadedTrack track)
    {
        SongQueue QueueItem = new SongQueue
        {
            Id = track.Id,
            Filename = track.Filename,
            TimeStamp = DateTime.Now
        };
        await _context.SongQueue.AddAsync(QueueItem);
        await _context.SaveChangesAsync();
        RecheckPlaybackQueue();
    }

    public async void RecheckPlaybackQueue()
    {
        if (_ffmpegProcess != null)
            return;

        bool QueueHasSongs = await _context.SongQueue.AnyAsync();

        if (!QueueHasSongs)
            return;

        SongQueue nextInQueue = await _context.SongQueue
            .OrderBy(q => q.TimeStamp)
            .FirstAsync();

        bool trackDownloaded = await _context.DownloadedTracks
                .AnyAsync(dt => dt.Filename == nextInQueue.Filename);

        if (!trackDownloaded)
        {
            // Could do some better error handling here to download the track so it exists.
            return;
        }

        _context.SongQueue.Remove(nextInQueue);
        await _context.SaveChangesAsync();

        Play(nextInQueue.Id);
    }

    public async Task Play(int id)
    {
        var dbTrack = await _context.DownloadedTracks.Where(t => t.Id == id).FirstOrDefaultAsync();

        if (dbTrack == null)
            return;

        if (!_ioService.TrackExists(dbTrack.Filename))
            return;

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
        RecheckPlaybackQueue();
    }

    public async Task<bool> Skip()
    {
        if (_ffmpegProcess == null)
            return false;
        else
        {
            await _voiceClientService.Client.CloseAsync();
            _ffmpegProcess.Close();
            _ffmpegProcess = null;
            RecheckPlaybackQueue();
            return true;
        }
    }

    public async Task<int> CountSongsInQueue() =>
        await _context.SongQueue.CountAsync();

    public async Task<IEnumerable<SongQueue>> GetQueuedSongs() =>
        await _context.SongQueue.Include(sq => sq.DownloadedTrack).ToListAsync();
}

