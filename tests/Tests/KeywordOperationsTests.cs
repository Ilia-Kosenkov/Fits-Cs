using System;
using FitsCs;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class KeywordOperationsTests
    {
        static ReadOnlySpan<char> LoremIpsum() =>
            @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
                .AsSpan();

        [Test]
        public void Test_Continue_Text_Roundtrips()
        {
            var keys = FitsKey.ToContinueKeys(LoremIpsum(), ReadOnlySpan<char>.Empty, @"LOREM");
            Assert.That(keys.Length, Is.EqualTo(7));
            Assert.That(keys[0] is IFitsValue<string> key && key.Name == @"LOREM");

            var roundTrip = FitsKey.ParseContinuedString(keys);
            Assert.That(string.IsNullOrEmpty(roundTrip.Comment));
            Assert.That(roundTrip.Text.AsSpan().SequenceEqual(LoremIpsum()));
        }

        [Test]
        public void Test_Continue_Comment_Roundtrips()
        {
            var keys = FitsKey.ToContinueKeys(ReadOnlySpan<char>.Empty, LoremIpsum(), @"LOREM");
            Assert.That(keys.Length, Is.EqualTo(7));
            Assert.That(keys[0] is IFitsValue<string> key && key.Name == @"LOREM");

            var roundTrip = FitsKey.ParseContinuedString(keys, true);
            Assert.That(string.IsNullOrEmpty(roundTrip.Text));
            Assert.That(roundTrip.Comment.AsSpan().SequenceEqual(LoremIpsum()));
        }

        [Test]
        public void Test_Continue_TextComment_Roundtrips()
        {
            var keys = FitsKey.ToContinueKeys(LoremIpsum(), LoremIpsum(), @"LOREM");
            Assert.That(keys.Length, Is.EqualTo(14));
            Assert.That(keys[0] is IFitsValue<string> key && key.Name == @"LOREM");

            var roundTrip = FitsKey.ParseContinuedString(keys, true);
            Assert.That(roundTrip.Text.AsSpan().SequenceEqual(LoremIpsum()));
            Assert.That(roundTrip.Comment.AsSpan().SequenceEqual(LoremIpsum()));
        }

        [Test]
        public void Test_CommentKeys_Text_Roundtrips()
        {
            var keys = FitsKey.ToComments(LoremIpsum());
            Assert.That(keys.Length, Is.EqualTo(7));
            var roundTrip = FitsKey.ParseCommentString(keys);
            Assert.That(roundTrip.AsSpan().SequenceEqual(roundTrip.AsSpan()));
        }
    }
}
