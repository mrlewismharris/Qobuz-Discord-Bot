using QobuzDiscordBot.Models.Dtos;

namespace QobuzDiscordBot.Models.ViewModels
{
    public class UserSearch
    {
        public ulong UserId { get; set; }

        public string SearchQuery { get; set; } = "";

        public IEnumerable<TrackDto> Results { get; set; } = new List<TrackDto>();

        public int Offset { get; set; } = 0;

        public DateTime DateTime { get; set; }
    }
}
