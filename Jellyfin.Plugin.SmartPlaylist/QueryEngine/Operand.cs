using System.Collections.Generic;

namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
    public class Operand
    {
        public Operand(string name)
        {
            Actors = new List<string>();
            Composers = new List<string>();
            CommunityRating = 0;
            CriticRating = 0;
            Directors = new List<string>();
            Genres = new List<string>();
            GuestStars = new List<string>();
            Name = name;
            FolderPath = "";
            PremiereDate = 0;
            Producers = new List<string>();
            Studios = new List<string>();
	    Tags = new List<string>();
            Writers = new List<string>();
            MediaType = "";
            Album = "";
            DateCreated = 0;
            DateLastRefreshed = 0;
            DateLastSaved = 0;
            DateModified = 0;
        }

        public List<string> Actors { get; set; }
        public List<string> Composers { get; set; }
        public float CommunityRating { get; set; }
        public float CriticRating { get; set; }
        public List<string> Directors { get; set; }
        public List<string> Genres { get; set; }
        public List<string> GuestStars { get; set; }
        public bool IsPlayed { get; set; }
        public string Name { get; set; }
        public string FolderPath { get; set; }
        public double PremiereDate { get; set; }
        public List<string> Producers { get; set; }
        public List<string> Studios { get; set; }
	public List<string> Tags { get; set; }
        public List<string> Writers { get; set; }
        public string MediaType { get; set; }
        public string Album { get; set; }
        public double DateCreated { get; set; }
        public double DateLastRefreshed { get; set; }
        public double DateLastSaved { get; set; }
        public double DateModified { get; set; }
    }
}
