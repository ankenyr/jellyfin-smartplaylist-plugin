using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using Jellyfin.Data.Entities;
using System.Linq;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
    class OperandFactory
    {
        public static Operand GetMediaType(ILibraryManager libraryManager, BaseItem baseItem, User user)
        {

            var operand = new Operand(baseItem.Name);
            var directors = new List<string> { };
            var people = libraryManager.GetPeople(baseItem);
            var foo = people.Any();
            if (people.Any())
            {
                operand.Directors = people.Where(x => x.Type.Equals("Director")).Select(x => x.Name).ToList();
                operand.Actors = people.Where(x => x.Type.Equals("Actor")).Select(x => x.Name).ToList();
            }
            operand.Genres = baseItem.Genres.ToList();
            operand.IsPlayed = baseItem.IsPlayed(user);

            if (baseItem.PremiereDate.HasValue)
            {
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var dt = DateTime.Parse(baseItem.PremiereDate.ToString());
                TimeSpan diff = dt.ToUniversalTime() - origin;
                operand.PremiereDate = Math.Floor(diff.TotalSeconds);
            }
            return operand;
        }
    }
}
