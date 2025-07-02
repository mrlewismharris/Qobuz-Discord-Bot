using Microsoft.EntityFrameworkCore;
using NetCord.Gateway.Voice;
using QobuzDiscordBot.Models.DbModels;
using QobuzDiscordBot.Models.Dtos;
using System.Diagnostics;
using System.Threading.Tasks;

namespace QobuzDiscordBot.Services
{
    public class PlaybackService
    {
        public ICollection<DownloadedTrack> _playbackQueue;
        public bool _isPlaying;
        public readonly DataContext _context;
        public readonly IOService _ioService;
        private VoiceClientService _voiceClientService;

        public PlaybackService(DataContext context, IOService ioService, VoiceClientService voiceClientService)
        {
            _playbackQueue = new List<DownloadedTrack>();
            _isPlaying = false;
            _context = context;
            _ioService = ioService;
            _voiceClientService = voiceClientService;
        }

        public void Add(DownloadedTrack track)
        {
            _playbackQueue.Add(track);
            RecheckPlaybackQueue();
        }

        public void RecheckPlaybackQueue()
        {
            if (_isPlaying)
                return;
            var nextTrack = _playbackQueue.First();
            _playbackQueue.Remove(nextTrack);
            Play(nextTrack.Id);
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
            arguments.Add("-i");
            arguments.Add(_ioService.GetFullPathFromFileName(dbTrack.Filename));
            arguments.Add("-ac");
            arguments.Add("2");
            arguments.Add("-f");
            arguments.Add("s16le");
            arguments.Add("-ar");
            arguments.Add("48000");
            arguments.Add("pipe:1");
            var ffmpeg = Process.Start(startInfo);
            if (ffmpeg == null)
                return;
            await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream);
            await stream.FlushAsync();
            RecheckPlaybackQueue();
        }
    }
}
