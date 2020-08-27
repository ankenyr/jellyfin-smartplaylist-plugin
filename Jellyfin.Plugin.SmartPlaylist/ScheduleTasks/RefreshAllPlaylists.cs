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
using Jellyfin.Plugin.SmartPlaylist.QueryEngine;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.SmartPlaylist.ScheduleTasks
{
    public class RefreshAllPlaylists : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IDtoService _dtoService;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IPlaylistManager _playlistManager;
        private readonly IProviderManager _providerManager;
        private readonly IServerApplicationPaths _serverApplicationPaths;
        private readonly ISmartPlaylistFileSystem _plFileSystem;
        private readonly ISmartPlaylistStore _plStore;
        private readonly IUserManager _userManager;
        public RefreshAllPlaylists(
            IDtoService dtoService,
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
            
            _dtoService = dtoService;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _libraryManager = libraryManager;
            _logger = logger;
            _playlistManager = playlistManager;
            _providerManager = providerManager;
            _serverApplicationPaths = serverApplicationPaths;
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
                    IntervalTicks = TimeSpan.FromMinutes(1).Ticks,
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

        private QueryResult<BaseItem> GetAllUserMedia(User user)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = SupportedItemTypeNames,
                Recursive = true,
            };
            return _libraryManager.GetItemsResult(query);
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var dtos = _plStore.GetAllSmartPlaylistsAsync();
            dtos.Wait();
            foreach (var dto in dtos.Result)
            {
                var user = _userManager.GetUserByName(dto.User);
                List<Playlist> p;
                try 
                {
                    var playlists = _playlistManager.GetPlaylists(user.Id);
                    p = playlists.Where(x => x.Id.ToString().Replace("-", "") == dto.Id).ToList();
                    dto.Expressions = Engine.FixRules(dto.Expressions);
                }
                catch (NullReferenceException ex)
                {
                    _logger.LogError(ex, "No user named {0} found, please fix playlist {1}", dto.User, dto.Name);
                    continue;
                }
               
                var compiledRules = dto.Expressions.Select(r => Engine.CompileRule<Operand>(r)).ToList();
                if (dto.Id == null | p.Count() == 0)
                {
                    _logger.LogInformation("Playlist ID not set, creating new playlist");
                    var plid = CreateNewPlaylist(dto, user);
                    dto.Id = plid;
                    _plStore.Save(dto);
                }
                var new_items = new List<BaseItem> { };
                var results = GetAllUserMedia(user);
                foreach (var i in results.Items)
                {
                    var operand = OperandFactory.GetMediaType(_libraryManager, i, user);

                    if (compiledRules.All(rule => rule(operand)))
                    {
                        new_items.Add(i);
                    }
                }
                var playlist = p.First();
                var query = new InternalItemsQuery(user)
                {
                    IncludeItemTypes = SupportedItemTypeNames,
                    Recursive = true,
                };
                var plitems = playlist.GetChildren(user, false, query).ToList().Take(dto.MaxItems);
                var toremove = plitems.Select(x => x.Id.ToString()).ToList();
                RemoveFromPlaylist(playlist.Id.ToString(), toremove);
                _playlistManager.AddToPlaylist(playlist.Id.ToString(), new_items.Select(x => x.Id).ToArray(), user.Id);
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