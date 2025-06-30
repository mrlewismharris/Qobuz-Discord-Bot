namespace QobuzDiscordBot.Models.ViewModels
{
    public class UserSearch
    {
        public ulong UserId { get; set; }

        public string SearchQuery { get; set; } = "";

        public IEnumerable<TrackDto> Results { get; set; } = new List<TrackDto>();

        //if user selects "load more" option, offset will store which pagination page they are on
        public int Offset { get; set; } = 0;

        public DateTime DateTime { get; set; }
    }
}
