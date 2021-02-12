using System.Linq;
using MethodCallCommand.Utils;
using NUnit.Framework;

namespace MethodCallCommand.Tests
{
    [TestFixture]
    public class CommandParserTests
    {
        [Test]
        public void TestParseEmpty()
        {
            Assert.IsFalse(CommandParser.Parse("").Any());
        }

        [Test]
        public void TestParseIdentifier()
        {
            var segments = CommandParser.Parse("hello").ToArray();
            Assert.AreEqual(1, segments.Length);
            Assert.AreEqual(CommandParser.SegmentType.Identifier, segments[0].Type);
        }

        [Test]
        public void TestParseIdentifierAndEscapedQuote()
        {
            var segments = CommandParser.Parse(@"hello ""wor\""ld""").ToArray();
            Assert.AreEqual(2, segments.Length);
            Assert.AreEqual(CommandParser.SegmentType.Identifier, segments[0].Type);
            Assert.AreEqual("hello", segments[0].Text);
            Assert.AreEqual(CommandParser.SegmentType.String, segments[1].Type);
            Assert.AreEqual(@"wor""ld", segments[1].Text);
        }

        [Test]
        public void TestParseIdentifierAndEscapedSlashAndQuote()
        {
            var segments = CommandParser.Parse("hello \"wor\\\\\\\"ld").ToArray();
            Assert.AreEqual(2, segments.Length);
            Assert.AreEqual(CommandParser.SegmentType.Identifier, segments[0].Type);
            Assert.AreEqual("hello", segments[0].Text);
            Assert.AreEqual(CommandParser.SegmentType.String, segments[1].Type);
            Assert.AreEqual(@"wor\""ld", segments[1].Text);
        }

        [Test]
        public void TestParseIdentifierAndEscapedSlashAndQuoteWithPostfix()
        {
            var segments = CommandParser.Parse("hello \"wor\\\\\\\"ld\" some 42 content").ToArray();
            Assert.AreEqual(5, segments.Length);
            Assert.AreEqual(CommandParser.SegmentType.Identifier, segments[0].Type);
            Assert.AreEqual("hello", segments[0].Text);
            Assert.AreEqual(CommandParser.SegmentType.String, segments[1].Type);
            Assert.AreEqual(@"wor\""ld", segments[1].Text);
            Assert.AreEqual(CommandParser.SegmentType.Identifier, segments[2].Type);
            Assert.AreEqual(@"some", segments[2].Text);
            Assert.AreEqual(CommandParser.SegmentType.Int, segments[3].Type);
            Assert.AreEqual(@"42", segments[3].Text);
            Assert.AreEqual(CommandParser.SegmentType.Identifier, segments[4].Type);
            Assert.AreEqual(@"content", segments[4].Text);
        }

        [Test]
        public void TestParseIdentifierAndString()
        {
            var segments = CommandParser.Parse(@"hello ""world""").ToArray();
            Assert.AreEqual(2, segments.Length);
            Assert.AreEqual(CommandParser.SegmentType.Identifier, segments[0].Type);
            Assert.AreEqual("hello", segments[0].Text);
            Assert.AreEqual(CommandParser.SegmentType.String, segments[1].Type);
            Assert.AreEqual("world", segments[1].Text);
        }

        [Test]
        public void TestParseIdentifierWithWhiteSpacePostfix()
        {
            var segments = CommandParser.Parse("hello    ").ToArray();
            Assert.AreEqual(1, segments.Length);
            Assert.AreEqual(CommandParser.SegmentType.Identifier, segments[0].Type);
        }

        [Test]
        public void TestParseIdentifierWithWhiteSpacePrefix()
        {
            var segments = CommandParser.Parse("   hello").ToArray();
            Assert.AreEqual(1, segments.Length);
            Assert.AreEqual(CommandParser.SegmentType.Identifier, segments[0].Type);
        }

        [Test]
        public void TestParseString()
        {
            var segments = CommandParser.Parse(@"""world""").ToArray();
            Assert.AreEqual(1, segments.Length);
            Assert.AreEqual(CommandParser.SegmentType.String, segments[0].Type);
            Assert.AreEqual("world", segments[0].Text);
        }
    }
}
