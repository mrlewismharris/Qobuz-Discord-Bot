using QobuzDiscordBot.Models.ViewModels;

namespace QobuzDiscordBot.Services
{
    public class SearchCacheService
    {
        public ICollection<UserSearch> Cache { get; } = new List<UserSearch>();
    }
}
