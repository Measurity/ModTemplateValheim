using System.Collections.Generic;
using MethodCallCommand.Utils;
using NUnit.Framework;

namespace MethodCallCommand.Tests
{
    [TestFixture]
    public class ArrayUtilsTests
    {
        [Test]
        public void TestParseObjectToArray()
        {
            object test = new List<CommandParser.CommandSegment>
            {
                new CommandParser.CommandSegment(CommandParser.SegmentType.String, "my string"),
                new CommandParser.CommandSegment(CommandParser.SegmentType.Int, "42"),
                null,
                new CommandParser.CommandSegment(CommandParser.SegmentType.Identifier, "player")
            };

            Assert.AreEqual($"my string, 42, {ArrayUtils.NullString}, player",
                string.Join(", ", ArrayUtils.ToStringArray(test)));
        }
    }
}
