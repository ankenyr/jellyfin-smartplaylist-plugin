using System.Collections.Generic;
using Jellyfin.Plugin.SmartPlaylist;
using Jellyfin.Plugin.SmartPlaylist.QueryEngine;
using Xunit;

namespace Tests
{
    public class SmartPlaylistTest
    {
        [Fact]
        public void DtoToSmartPlaylist()
        {
            var dto = new SmartPlaylistDto
            {
                Id = "87ccaa10-f801-4a7a-be40-46ede34adb22",
                Name = "Foo",
                User = "Rob"
            };

            var es = new ExpressionSet
            {
                Expressions = new List<Expression>
                {
                    new("foo", "bar", "biz")
                }
            };
            dto.ExpressionSets = new List<ExpressionSet>
            {
                es
            };
            dto.Order = new OrderDto
            {
                Name = "Release Date Descending"
            };
            var smartPlaylist = new SmartPlaylist(dto);

            Assert.Equal(1000, smartPlaylist.MaxItems);
            Assert.Equal("87ccaa10-f801-4a7a-be40-46ede34adb22", smartPlaylist.Id);
            Assert.Equal("Foo", smartPlaylist.Name);
            Assert.Equal("Rob", smartPlaylist.User);
            Assert.Equal("foo", smartPlaylist.ExpressionSets[0].Expressions[0].MemberName);
            Assert.Equal("bar", smartPlaylist.ExpressionSets[0].Expressions[0].Operator);
            Assert.Equal("biz", smartPlaylist.ExpressionSets[0].Expressions[0].TargetValue);
            Assert.Equal("PremiereDateOrderDesc", smartPlaylist.Order.GetType().Name);
        }
    }
}