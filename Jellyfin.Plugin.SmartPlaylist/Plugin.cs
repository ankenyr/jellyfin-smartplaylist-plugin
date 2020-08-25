using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Querying;
using MediaBrowser.Controller.Entities;
using Jellyfin.Data.Entities;


namespace Jellyfin.Plugin.SmartPlaylist
{
    public class Plugin : BasePlugin<BasePluginConfiguration>, IHasWebPages
    {
        public Plugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer
            ) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }


        public override Guid Id => Guid.Parse("3C96F5BC-4182-4B86-B05D-F730F2611E45");

        public override string Name => "SmartPlaylist";

        public override string Description => "Allow to define smart playlist rules.";

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    //Name = "smartplaylist.html",
                    //EmbeddedResourcePath = string.Format("{0}.Configuration.smartplaylist.html", GetType().Namespace),
                    
                }
            };
        }

    }
}
