namespace QobuzDiscordBot.Models.Dtos
{
    public class TrackDto
    {
        public int? Id { get; set; }

        public string Title { get; set; }

        public long? Duration { get; set; }

        public string Performer { get; set; }

        public string Version { get; set; }

        public string DisplayInfo => $"{Title}{(string.IsNullOrWhiteSpace(Version) ? "" : $" ({Version})")} - {Performer} ({
            TimeSpan.FromSeconds((int)Duration!).Minutes}:{TimeSpan.FromSeconds((int)Duration!).Seconds})";
    }
}
