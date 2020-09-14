﻿using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using Jellyfin.Data.Entities;
using System.Linq;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
    class OperandFactory
    {
        // Returns a specific operand povided a baseitem, user, and library manager object.
        public static Operand GetMediaType(ILibraryManager libraryManager, BaseItem baseItem, User user)
        {

            var operand = new Operand(baseItem.Name);
            
            // Unused var
            //var directors = new List<string> { };
            
            var people = libraryManager.GetPeople(baseItem);
            if (people.Any())
            {
                // Maps to MediaBrowser.Model.Entities.PersonType
                operand.Actors = people.Where(x => x.Type.Equals("Actor")).Select(x => x.Name).ToList();
                operand.Composers = people.Where(x => x.Type.Equals("Composer")).Select(x => x.Name).ToList();
                operand.Directors = people.Where(x => x.Type.Equals("Director")).Select(x => x.Name).ToList();
                operand.GuestStars = people.Where(x => x.Type.Equals("GuestStar")).Select(x => x.Name).ToList();
                operand.Producers = people.Where(x => x.Type.Equals("Producer")).Select(x => x.Name).ToList();
                operand.Writers = people.Where(x => x.Type.Equals("Writer")).Select(x => x.Name).ToList();
            }
            operand.Genres = baseItem.Genres.ToList();
            operand.IsPlayed = baseItem.IsPlayed(user);
            operand.Studios = baseItem.Studios.ToList();
            operand.CommunityRating = baseItem.CommunityRating.GetValueOrDefault();
            operand.CriticRating = baseItem.CriticRating.GetValueOrDefault();

            if (baseItem.PremiereDate.HasValue)
            {
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var dt = DateTime.Parse(baseItem.PremiereDate.ToString());
                TimeSpan diff = dt.ToUniversalTime() - origin;
                operand.PremiereDate = Math.Floor(diff.TotalSeconds);
            }

            Guid[] ids = new Guid[] {
                 baseItem.Id
            };

            var artists = libraryManager.GetAlbumArtists(new InternalItemsQuery(user)
            {
                ItemIds = ids
            });

            var artistNames = artists.Items.Select(i => i.Item1.Name).ToList();
            operand.Artists = artistNames;

            if (baseItem.DateCreated != null)
            {
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var dt = DateTime.Parse(baseItem.DateCreated.ToString());
                TimeSpan diff = dt.ToUniversalTime() - origin;
                operand.DateCreated = Math.Floor(diff.TotalSeconds);
            }

            // operand.Album = baseItem.Album;

            // album name
            // date last played
            // DONE - date added to jf
            // DONE - song name
            // play count - field might not exist

            // Song/Episode/Movie name
            operand.ItemName = baseItem.Name;

            operand.FolderPath = baseItem.ContainingFolderPath;
            return operand;
        }
    }
}
