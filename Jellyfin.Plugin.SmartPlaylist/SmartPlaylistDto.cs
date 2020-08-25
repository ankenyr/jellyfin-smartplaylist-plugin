using Jellyfin.Plugin.SmartPlaylist.QueryEngine;
using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.SmartPlaylist
{
    [Serializable]
    public class SmartPlaylistDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public string User { get; set; }
        public List<Expression> Expressions { get; set; }
    }
}