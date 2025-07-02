using Microsoft.Extensions.Configuration;

namespace QobuzDiscordBot.Services
{
    public class IOService
    {
        private readonly string _rootPath;
        private readonly int _storageLimit;
        private readonly DataContext _context;
        private readonly IConfiguration _config;

        public IOService(IConfiguration config, DataContext context)
        {
            _context = context;
            _rootPath = Directory.GetCurrentDirectory();
            _config = config;
            _storageLimit = -1;
            var envStorageLimit = config["STORAGE_LIMIT"];
            if (envStorageLimit != null)
                if (int.TryParse(envStorageLimit, out int storageLimit))
                    _storageLimit = storageLimit;
                else
                    throw new Exception("Environment variable \"STORAGE_LIMIT\" must be of type int, or could not be parsed to an int.");
        }

        public bool StorageLimitReached() => _storageLimit > -1 && _storageLimit < CountStoredTracks();

        public string GetFullPathFromFileName(string filename) => Path.Combine(_rootPath, "Music", filename);

        public int CountStoredTracks() => Directory.EnumerateFiles(Path.Combine(_rootPath, "Music")).Count();

        public int GetStorageLimit() => _storageLimit;

        public bool TrackExists(string filename) => File.Exists(Path.Combine(_rootPath, "Music", filename));
    }
}
