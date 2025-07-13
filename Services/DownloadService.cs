using Microsoft.EntityFrameworkCore;
using NetCord.Services.Commands;
using QobuzApiSharp.Service;
using QobuzDiscordBot.Models.DbModels;
using QobuzDiscordBot.Models.Dtos;
using QobuzDiscordBot.Models.ViewModels;

namespace QobuzDiscordBot.Services;

public class DownloadService
{
    private readonly string _rootPath;
    private ICollection<QueuedTrack> _downloadQueue;
    private bool _isDownloading;
    private readonly DataContext _context;
    private readonly IOService _ioService;
    private readonly QobuzApiService _qobuzApi;
    private readonly HttpClient _httpClient;
    private readonly PlaybackService _playbackService;

    public DownloadService(DataContext context, IOService ioService, QobuzApiService qobuzApi, HttpClient httpClient, PlaybackService playbackService)
    {
        _rootPath = Directory.GetCurrentDirectory();
        _downloadQueue = new List<QueuedTrack>();
        _context = context;
        _ioService = ioService;
        _isDownloading = false;
        _qobuzApi = qobuzApi;
        _httpClient = httpClient;
        _playbackService = playbackService;
    }

    /// <summary>
    /// Add a track to the download queue. If song is already downloaded, add to playback queue, if not add to download queue.
    /// </summary>
    public async Task Add(TrackDto track, DiscordUser user, CommandContext? context = null)
    {
        if (_downloadQueue.Any() || _isDownloading || _ioService.StorageLimitReached())
        {
            _downloadQueue.Add(new QueuedTrack
            {
                Track = track,
                QueuedBy = user,
                QueuedAt = DateTime.Now,
            });
            if (context != null)
                context.Message.SendAsync($"Track added to download queue (position: {_downloadQueue.Count()}).");
        }
        Download(track, context);
        RecheckDownloadQueue();
        var queuePosition = _downloadQueue.Count() + await _playbackService.CountSongsInQueue();
        var eta = TimeSpan.FromSeconds(_downloadQueue.Select(q => q.Track.Duration.Value).Sum() + (await _playbackService.GetQueuedSongs()).Select(s => s.DownloadedTrack.Duration).Sum()).TotalMinutes;
        if ((await CheckAlreadyDownloaded(track.Id.Value)) == null)
            if (context != null)
                context.Message.SendAsync($"Downloading \"{track.Performer} - {track.Title}\"");
        else
            if (context != null)
                context.Message.SendAsync($"{track.Performer} - {track.Title} has already been downloaded. Adding Track to queue (Position: {queuePosition}, playing in: {eta} minutes)");
    }

    public void RecheckDownloadQueue()
    {
        if (_downloadQueue.Any() && !_isDownloading && !_ioService.StorageLimitReached())
        {
            var nextTrack = _downloadQueue.OrderBy(t => t.QueuedAt).First();
            _downloadQueue.Remove(nextTrack);
            Download(nextTrack.Track);
        }
    }

    public async Task Download(TrackDto track, CommandContext? context = null)
    {
        _isDownloading = true;
        var existingTrack = await CheckAlreadyDownloaded(track.Id.Value);
        if (existingTrack != null)
        {
            _playbackService.Add(existingTrack, context);
            return;
        }
        var fileUrl = _qobuzApi.GetTrackFileUrl(track.Id.ToString(), "6");
        await using var stream = await _httpClient.GetStreamAsync(fileUrl.Url);
        Console.WriteLine(fileUrl.Url);
        var filePath = Path.Combine(_rootPath, "Music", $"{track.Performer} - {track.Title} ({track.Id}).flac");
        if (!Directory.Exists(Path.Combine(_rootPath, "Music")))
            Directory.CreateDirectory(Path.Combine(_rootPath, "Music"));
        await using var file = File.Create(filePath);
        await stream.CopyToAsync(file);
        var dbTrack = new DownloadedTrack
        {
            Id = track.Id.Value,
            Title = track.Title,
            Performer = track.Performer,
            Version = track.Version,
            PlayCount = 0,
            Filename = filePath,
            Duration = track.Duration ?? 0
        };
        await _context.DownloadedTracks.AddAsync(dbTrack);
        await _context.SaveChangesAsync();
        _playbackService.Add(dbTrack, context);
        var queueTrack = _downloadQueue.FirstOrDefault(t => t.Track.Id == dbTrack.Id);
        if (queueTrack != null)
            _downloadQueue.Remove(queueTrack);
        _isDownloading = false;
        RecheckDownloadQueue();
    }

    public async Task<DownloadedTrack?> CheckAlreadyDownloaded(int id)
    {
        var dbTrack = await _context.DownloadedTracks.Where(t => t.Id == id).FirstOrDefaultAsync();
        if (dbTrack == null)
            return null;
        else if (_ioService.TrackExists(dbTrack.Filename))
            return dbTrack;
        _context.DownloadedTracks.Remove(dbTrack);
        await _context.SaveChangesAsync();
        return dbTrack;
    }

    public void ClearDownloadQueue() => _downloadQueue.Clear();
}

