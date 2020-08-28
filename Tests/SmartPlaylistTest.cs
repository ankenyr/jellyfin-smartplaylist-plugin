using System;
using Xunit;
using Jellyfin.Plugin.SmartPlaylist;
using Jellyfin.Plugin.SmartPlaylist.QueryEngine;
using System.Collections.Generic;

namespace Tests
{
    public class SmartPlaylistTest
    {
        [Fact]
        public void DtoToSmartPlaylist()
        {
            var dto = new SmartPlaylistDto();
            dto.Id = "87ccaa10-f801-4a7a-be40-46ede34adb22";
            dto.Name = "Foo";
            dto.User = "Rob";
            dto.Expressions = new List<Expression>
            {
                new Expression("foo", "bar", "biz")
            };
            dto.Order = new OrderDto
            {
                Name = "Release Date Descending"
            };
            SmartPlaylist smart_playlist = new SmartPlaylist(dto);

            Assert.Equal(1000, smart_playlist.MaxItems);
            Assert.Equal("87ccaa10-f801-4a7a-be40-46ede34adb22", smart_playlist.Id);
            Assert.Equal("Foo", smart_playlist.Name);
            Assert.Equal("Rob", smart_playlist.User);

            Assert.Equal("foo", smart_playlist.Expressions[0].MemberName);
            Assert.Equal("bar",smart_playlist.Expressions[0].Operator);
            Assert.Equal("biz",smart_playlist.Expressions[0].TargetValue);
            Assert.Equal("PremiereDateOrderDesc", smart_playlist.Order.GetType().Name);
        }
    }
}
