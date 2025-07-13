using System.ComponentModel.DataAnnotations.Schema;

namespace QobuzDiscordBot.Models.DbModels;

public class SongQueue
{
    public int Id { get; set; }

    [ForeignKey("Id")]
    public virtual DownloadedTrack DownloadedTrack { get; set; }

    public required string Filename { get; set; }

    public required DateTime TimeStamp { get; set; }
}