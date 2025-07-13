namespace QobuzDiscordBot.Models.DbModels
{
    public class DownloadedTrack
    {
        public int Id { get; set; }

        public int PlayCount { get; set; }

        public string Filename { get; set; }

        public long Duration { get; set; }
    }
}
