using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.SmartPlaylist.QueryEngine;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.SmartPlaylist
{
    public class SmartPlaylist
    {
        public SmartPlaylist(SmartPlaylistDto dto)
        {
            Id = dto.Id;
            Name = dto.Name;
            FileName = dto.FileName;
            User = dto.User;
            ExpressionSets = Engine.FixRuleSets(dto.ExpressionSets);
            if (dto.MaxItems > 0)
                MaxItems = dto.MaxItems;
            else
                MaxItems = 1000;

            switch (dto.Order.Name)
            {
                //ToDo It would be nice to move to automapper and create a better way to map this.
                // Could also use DefinedLimitOrders from emby version.
                case "NoOrder":
                    Order = new NoOrder();
                    break;
                case "Release Date Ascending":
                    Order = new PremiereDateOrder();
                    break;
                case "Release Date Descending":
                    Order = new PremiereDateOrderDesc();
                    break;
                default:
                    Order = new NoOrder();
                    break;
            }
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public string User { get; set; }
        public List<ExpressionSet> ExpressionSets { get; set; }
        public int MaxItems { get; set; }
        public Order Order { get; set; }

        private List<List<Func<Operand, bool>>> CompileRuleSets()
        {
            var compiledRuleSets = new List<List<Func<Operand, bool>>>();
            foreach (var set in ExpressionSets)
                compiledRuleSets.Add(set.Expressions.Select(r => Engine.CompileRule<Operand>(r)).ToList());
            return compiledRuleSets;
        }

        // Returns the ID's of the items, if order is provided the IDs are sorted.
        public IEnumerable<Guid> FilterPlaylistItems(IEnumerable<BaseItem> items, ILibraryManager libraryManager,
            User user)
        {
            var results = new List<BaseItem>();

            var compiledRules = CompileRuleSets();
            foreach (var i in items)
            {
                var operand = OperandFactory.GetMediaType(libraryManager, i, user);

                if (compiledRules.Any(set => set.All(rule => rule(operand)))) results.Add(i);
            }

            return Order.OrderBy(results).Select(x => x.Id);
        }

        private static void Validate()
        {
            //Todo create validation for constructor
        }
    }

    public abstract class Order
    {
        public abstract string Name { get; }

        public virtual IEnumerable<BaseItem> OrderBy(IEnumerable<BaseItem> items)
        {
            return items;
        }
    }

    public class NoOrder : Order
    {
        public override string Name => "NoOrder";
    }

    public class PremiereDateOrder : Order
    {
        public override string Name => "Release Date Ascending";

        public override IEnumerable<BaseItem> OrderBy(IEnumerable<BaseItem> items)
        {
            return items.OrderBy(x => x.PremiereDate);
        }
    }

    public class PremiereDateOrderDesc : Order
    {
        public override string Name => "Release Date Descending";

        public override IEnumerable<BaseItem> OrderBy(IEnumerable<BaseItem> items)
        {
            return items.OrderByDescending(x => x.PremiereDate);
        }
    }
}