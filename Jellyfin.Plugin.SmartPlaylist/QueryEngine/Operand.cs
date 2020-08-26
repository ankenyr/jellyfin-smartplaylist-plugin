using System.Collections.Generic;

namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
    public class Operand
    {
        public List<string> Actors { get; set; }
        public List<string> Directors { get; set; }
        public List<string> Genres { get; set; }
        public bool IsPlayed { get; set; }
        public string Name { get; set; }
        public double PremiereDate { get; set; }
        public Operand(string name)
        {
            Actors = new List<string>();
            Directors = new List<string>();
            Genres = new List<string>();
            Name = name;
        }
    }
}
