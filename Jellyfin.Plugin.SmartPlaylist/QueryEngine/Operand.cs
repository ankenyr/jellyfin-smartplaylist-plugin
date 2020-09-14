using System.Collections.Generic;

namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
    public class Operand
    {
        public List<string> Actors { get; set; }
        public List<string> Artists { get; set; }
        public List<string> Composers { get; set; }
        public float CommunityRating { get; set; }
        public float CriticRating { get; set; }
        public double DateCreated { get; set; }
        public List<string> Directors { get; set; }
        public List<string> Genres { get; set; }
        public List<string> GuestStars { get; set; }
        public bool IsPlayed { get; set; }
        public string ItemName { get; set; }
        public string Name { get; set; }
        public string FolderPath { get; set; }
        public double PremiereDate { get; set; }
        public List<string> Producers { get; set; }
        public List<string> Studios { get; set; }
        public List<string> Writers { get; set; }
        public Operand(string name)
        {
            Actors = new List<string>();
            Artists = new List<string>();
            Composers = new List<string>();
            CommunityRating = 0;
            CriticRating = 0;
            DateCreated = 0;
            Directors = new List<string>();
            Genres = new List<string>();
            GuestStars = new List<string>();
            ItemName = "";
            Name = name;
            FolderPath = "";
            PremiereDate = 0;
            Producers = new List<string>();
            Studios = new List<string>();
            Writers = new List<string>();
        }
    }
}
