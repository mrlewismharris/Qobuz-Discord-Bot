namespace QobuzDiscordBot.Models.ViewModels
{
    public class DiscordUser
    {
        public ulong Id { get; set; }

        public string Email { get; set; }

        public string Username { get; set; }

        public string? GlobalName { get; set; }
    }
}
