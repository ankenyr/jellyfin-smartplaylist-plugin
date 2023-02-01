using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SmartPlaylist
{
    public interface ISmartPlaylistStore
    {
        Task<SmartPlaylistDto> GetSmartPlaylistAsync(Guid smartPlaylistId);
        Task<SmartPlaylistDto[]> LoadPlaylistsAsync(Guid userId);
        Task<SmartPlaylistDto[]> GetAllSmartPlaylistsAsync();
        Task SaveAsync(SmartPlaylistDto smartPlaylist);
        void Delete(Guid userId, string smartPlaylistId);
    }

    public class SmartPlaylistStore : ISmartPlaylistStore
    {
        private readonly ISmartPlaylistFileSystem _fileSystem;

        public SmartPlaylistStore(ISmartPlaylistFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }


        public async Task<SmartPlaylistDto> GetSmartPlaylistAsync(Guid smartPlaylistId)
        {
            var fileName = _fileSystem.GetSmartPlaylistFilePath(smartPlaylistId.ToString());

            return await LoadPlaylistAsync(fileName).ConfigureAwait(false);
        }

        public async Task<SmartPlaylistDto[]> LoadPlaylistsAsync(Guid userId)
        {
            var deserializeTasks = _fileSystem.GetSmartPlaylistFilePaths(userId.ToString()).Select(LoadPlaylistAsync)
                .ToArray();

            await Task.WhenAll(deserializeTasks).ConfigureAwait(false);

            return deserializeTasks.Select(x => x.Result).ToArray();
        }

        public async Task<SmartPlaylistDto[]> GetAllSmartPlaylistsAsync()
        {
            var deserializeTasks = _fileSystem.GetAllSmartPlaylistFilePaths().Select(LoadPlaylistAsync).ToArray();

            await Task.WhenAll(deserializeTasks).ConfigureAwait(false);

            return deserializeTasks.Select(x => x.Result).ToArray();
        }

        public async Task SaveAsync(SmartPlaylistDto smartPlaylist)
        {
            var filePath = _fileSystem.GetSmartPlaylistPath(smartPlaylist.Id, smartPlaylist.FileName);
            await using var writer = File.Create(filePath);
            await JsonSerializer.SerializeAsync(writer, smartPlaylist).ConfigureAwait(false);
        }

        public void Delete(Guid userId, string smartPlaylistId)
        {
            var filePath = _fileSystem.GetSmartPlaylistPath(userId.ToString(), smartPlaylistId);
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        private async Task<SmartPlaylistDto> LoadPlaylistAsync(string filePath)
        {
            await using var reader = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<SmartPlaylistDto>(reader).ConfigureAwait(false);
        }
    }
}