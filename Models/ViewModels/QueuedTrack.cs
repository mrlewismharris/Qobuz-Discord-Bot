using QobuzDiscordBot.Models.Dtos;

namespace QobuzDiscordBot.Models.ViewModels
{
    public class QueuedTrack
    {
        public TrackDto Track { get; set; }

        public DiscordUser QueuedBy { get; set; }

        public DateTime QueuedAt { get; set; }
    }
}
