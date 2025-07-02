using Microsoft.EntityFrameworkCore;
using QobuzDiscordBot.Models.DbModels;

namespace QobuzDiscordBot
{
    public class DataContext : DbContext
    {


        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<DownloadedTrack> DownloadedTracks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
