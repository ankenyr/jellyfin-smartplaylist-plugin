using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartPlaylist.ScheduleTasks
{
    public class RefreshAllPlaylists : IScheduledTask, IConfigurableScheduledTask
    {
        public static readonly BaseItemKind[] SupportedItem =
            { BaseItemKind.Audio, BaseItemKind.Episode, BaseItemKind.Movie };

        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IPlaylistManager _playlistManager;
        private readonly ISmartPlaylistStore _plStore;
        private readonly IProviderManager _providerManager;
        private readonly IUserManager _userManager;

        public RefreshAllPlaylists(
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            ILogger<Plugin> logger,
            IPlaylistManager playlistManager,
            IProviderManager providerManager,
            IServerApplicationPaths serverApplicationPaths,
            IUserManager userManager
        )
        {
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
            _logger = logger;
            _playlistManager = playlistManager;
            _providerManager = providerManager;
            _userManager = userManager;

            ISmartPlaylistFileSystem plFileSystem = new SmartPlaylistFileSystem(serverApplicationPaths);
            _plStore = new SmartPlaylistStore(plFileSystem);

            _logger.LogInformation("Constructed Refresher ");
        }

        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => true;
        public string Key => nameof(RefreshAllPlaylists);
        public string Name => "Refresh all SmartPlaylists";
        public string Description => "Refresh all SmartPlaylists";
        public string Category => "Library";


        // TODO check for creation of schedule json file. Isn't created currently and won't execute until it is.
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    IntervalTicks = TimeSpan.FromMinutes(30).Ticks,
                    Type = TaskTriggerInfo.TriggerInterval
                }
            };
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var dtos = await _plStore.GetAllSmartPlaylistsAsync();
            foreach (var dto in dtos)
            {
                var smartPlaylist = new SmartPlaylist(dto);

                var user = _userManager.GetUserByName(smartPlaylist.User);
                List<Playlist> p;
                try
                {
                    var playlists = _playlistManager.GetPlaylists(user.Id);
                    p = playlists.Where(x => x.Id.ToString().Replace("-", "") == dto.Id).ToList();
                }
                catch (NullReferenceException ex)
                {
                    _logger.LogError(ex, "No user named {0} found, please fix playlist {1}", dto.User, dto.Name);
                    continue;
                }


                if ((dto.Id == null) | !p.Any())
                {
                    _logger.LogInformation("Playlist ID not set, creating new playlist");
                    var plId = CreateNewPlaylist(dto, user);
                    dto.Id = plId;
                    await _plStore.SaveAsync(dto);
                    var playlists = _playlistManager.GetPlaylists(user.Id);
                    p = playlists.Where(x => x.Id.ToString().Replace("-", "") == dto.Id).ToList();
                }

                var newItems = smartPlaylist.FilterPlaylistItems(GetAllUserMedia(user), _libraryManager, user);

                var playlist = p.First();
                var query = new InternalItemsQuery(user)
                {
                    IncludeItemTypes = SupportedItem,
                    Recursive = true
                };
                var plItems = playlist.GetChildren(user, false, query).ToList();

                var toRemove = plItems.Select(x => x.Id.ToString()).ToList();
                RemoveFromPlaylist(playlist.Id.ToString(), toRemove);
                await _playlistManager.AddToPlaylistAsync(playlist.Id, newItems.ToArray(), user.Id);
            }
        }

        private string CreateNewPlaylist(SmartPlaylistDto dto, User user)
        {
            var req = new PlaylistCreationRequest
            {
                Name = dto.Name,
                UserId = user.Id
            };
            var foo = _playlistManager.CreatePlaylist(req);
            return foo.Result.Id;
        }

        private IEnumerable<BaseItem> GetAllUserMedia(User user)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = SupportedItem,
                Recursive = true
            };

            return _libraryManager.GetItemsResult(query).Items;
        }

        // Real PlaylistManagers RemoveFromPlaylist needs an entry ID which seems to not work. Explore further and file a bug.
        public void RemoveFromPlaylist(string playlistId, IEnumerable<string> entryIds)
        {
            if (!(_libraryManager.GetItemById(playlistId) is Playlist playlist))
                throw new ArgumentException("No Playlist exists with the supplied Id");

            var children = playlist.GetManageableItems().ToList();

            var idList = entryIds.ToList();
            var removals = children.Where(i => idList.Contains(i.Item1.ItemId.ToString())).ToArray();

            playlist.LinkedChildren = children.Except(removals)
                .Select(i => i.Item1)
                .ToArray();
            playlist.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None);

            _providerManager.QueueRefresh(
                playlist.Id,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    ForceSave = true
                },
                RefreshPriority.High);
        }
    }
}