using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;


namespace Jellyfin.Plugin.SmartPlaylist.ScheduleTasks
{
    public class RefreshAllPlaylists : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IPlaylistManager _playlistManager;
        private readonly IProviderManager _providerManager;
        private readonly ISmartPlaylistFileSystem _plFileSystem;
        private readonly ISmartPlaylistStore _plStore;
        private readonly IUserManager _userManager;
        public RefreshAllPlaylists(
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
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
            
            _plFileSystem = new SmartPlaylistFileSystem(serverApplicationPaths);
            _plStore = new SmartPlaylistStore(jsonSerializer, _plFileSystem);
           
            _logger.LogInformation("Constructed Refresher ");
        }
        public static readonly Type[] SupportedItemTypes = { typeof(Audio), typeof(MediaBrowser.Controller.Entities.Movies.Movie), typeof(MediaBrowser.Controller.Entities.TV.Episode) };
        public static readonly string[] SupportedItemTypeNames = SupportedItemTypes.Select(x => x.Name).ToArray();
        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => true;
        public string Key => typeof(RefreshAllPlaylists).Name;
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
                    Type = TaskTriggerInfo.TriggerInterval,

                }
            };
        }

        private string CreateNewPlaylist(SmartPlaylistDto dto, User user)
        {
            var req = new PlaylistCreationRequest
            {
                Name = dto.Name,
                UserId = user.Id,

            };
            var foo = _playlistManager.CreatePlaylist(req);
            return foo.Result.Id;

        }

        private IEnumerable<BaseItem> GetAllUserMedia(User user)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = SupportedItemTypeNames,
                Recursive = true,
            };
            
            return (IEnumerable<BaseItem>)_libraryManager.GetItemsResult(query).Items;
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var dtos = _plStore.GetAllSmartPlaylistsAsync();
            dtos.Wait();
            foreach (var dto in dtos.Result)
            {
                SmartPlaylist smart_playlist = new SmartPlaylist(dto);

                var user = _userManager.GetUserByName(smart_playlist.User);
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


                if (dto.Id == null | p.Count() == 0)
                {
                    _logger.LogInformation("Playlist ID not set, creating new playlist");
                    var plid = CreateNewPlaylist(dto, user);
                    dto.Id = plid;
                    _plStore.Save(dto);
                    var playlists = _playlistManager.GetPlaylists(user.Id);
                    p = playlists.Where(x => x.Id.ToString().Replace("-", "") == dto.Id).ToList();
                }

                var new_items = smart_playlist.FilterPlaylistItems(GetAllUserMedia(user), _libraryManager, user);

                var playlist = p.First();
                var query = new InternalItemsQuery(user)
                {
                    IncludeItemTypes = SupportedItemTypeNames,
                    Recursive = true,
                };
                var plitems = playlist.GetChildren(user, false, query).ToList();

                var toremove = plitems.Select(x => x.Id.ToString()).ToList();
                RemoveFromPlaylist(playlist.Id.ToString(), toremove);
                _playlistManager.AddToPlaylist(playlist.Id.ToString(), new_items.ToArray(), user.Id);
            }
            return Task.CompletedTask;
        }

        // Real PlaylistManagers RemoveFromPlaylist needs an entry ID which seems to not work. Explore further and file a bug.
        public void RemoveFromPlaylist(string playlistId, IEnumerable<string> entryIds)
        {
            if (!(_libraryManager.GetItemById(playlistId) is Playlist playlist))
            {
                throw new ArgumentException("No Playlist exists with the supplied Id");
            }

            var children = playlist.GetManageableItems().ToList();

            var idList = entryIds.ToList();
            var removals = children.Where(i => idList.Contains(i.Item1.ItemId.ToString())).ToArray();

            playlist.LinkedChildren = children.Except(removals)
                .Select(i => i.Item1)
                .ToArray();

            playlist.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);

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