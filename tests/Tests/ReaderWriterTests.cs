using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitsCs;
using NUnit.Framework;

namespace Tests
{
    public struct FitsFileInfo
    { 
        public string Name { get; }
        public int NumUnits => NumKeywordsPerUnit.Length;
        public int[] NumKeywordsPerUnit { get; }

        public FitsFileInfo(string name, params int[] keysPerUnit)
        {
            Name = name;

            NumKeywordsPerUnit = keysPerUnit;
        }
    }

    public class ReaderWriter_TestCaseDataProvider
    {
        public static string TestDataDirectory { get; } = "TestData";

        private static IEnumerable<FitsFileInfo> Test_FitsReader_Data()
        {
            yield return new FitsFileInfo(
                @"testkeys.fits",
                180);

            yield return new FitsFileInfo(
                @"test64bit1.fits",
                36, 36, 36);

            yield return new FitsFileInfo(
                @"DDTSUVDATA.fits",
                180, 72);

            yield return new FitsFileInfo(
                @"FGSf64y0106m_a1f.fits",
                252, 72);

            yield return new FitsFileInfo(
                @"FOCx38i0101t_c0f.fits",
                144, 108);

            yield return new FitsFileInfo(
                @"NICMOSn4hk12010_mos.fits",
                252, 144, 72, 72, 72, 72);
        }

        public static IEnumerable Test_FitsReader_Provider =>
            Test_FitsReader_Data()
                .Select(x => new TestCaseData(x).SetName(
                    $"Test_FitsReader_{Path.GetFileNameWithoutExtension(x.Name)}"));
        public static IEnumerable Test_FitsReader_FitsWriter_Provider =>
            Test_FitsReader_Data()
                .Select(x => new TestCaseData(x).SetName(
                    $"Test_FitsReader_FitsWriter_{Path.GetFileNameWithoutExtension(x.Name)}"));

    }

    [TestFixture]
    public class ReaderWriterTests
    {
        [Theory]
        [TestCaseSource(
            typeof(ReaderWriter_TestCaseDataProvider),
            nameof(ReaderWriter_TestCaseDataProvider.Test_FitsReader_Provider))]
        public async Task Test_FitsReader(FitsFileInfo testCaseData)
        {
            var path = Path.Combine(ReaderWriter_TestCaseDataProvider.TestDataDirectory, testCaseData.Name);
            Assume.That(File.Exists(path));
            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            await using var fitsReader = new FitsReader(fileStream, 0, true);
            var counter = 0;

            await foreach (var block in fitsReader.EnumerateBlocksAsync())
            {
                Assume.That(counter, Is.LessThan(testCaseData.NumUnits));
                Assert.That(block.Keys.Count, Is.EqualTo(testCaseData.NumKeywordsPerUnit[counter++]));
                TestContext.Out.WriteLine((counter, block.Keys.Count));
            }

            Assert.That(counter, Is.EqualTo(testCaseData.NumUnits));
        }

        [Theory]
        [TestCaseSource(
            typeof(ReaderWriter_TestCaseDataProvider),
            nameof(ReaderWriter_TestCaseDataProvider.Test_FitsReader_FitsWriter_Provider))]
        public async Task Test_FitsReader_FitsWrite(FitsFileInfo testCaseData)
        {
            var path = Path.Combine(ReaderWriter_TestCaseDataProvider.TestDataDirectory, testCaseData.Name);
            Assume.That(File.Exists(path));
            List<Block> content;
            List<Block> reReadContent;

            await using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                await using var reader = new FitsReader(fileStream);
                content = await reader.EnumerateBlocksAsync().ToListAsync();
            }

            await using var memStr = new MemoryStream();
            {
                await using(var writer = new FitsWriter(memStr))
                    foreach (var block in content)
                        await writer.WriteBlockAsync(block);
                
                memStr.Seek(0, SeekOrigin.Begin);

                await using var reader = new FitsReader(memStr);

                reReadContent = await reader.EnumerateBlocksAsync().ToListAsync();
            }

            Assert.AreEqual(content.Count, reReadContent.Count);
            for (var i = 0; i < content.Count; i++)
            {
                CollectionAssert.AreEqual(content[i].Keys, reReadContent[i].Keys);
                Assert.IsTrue(content[i].RawData.SequenceEqual(reReadContent[i].RawData));
            }

        }

        [Test]
        public void Test_CommentSpaces()
        {
            var bytes = new byte[80];
            var bSpan = bytes.AsSpan();
            bSpan.Fill((byte)' ');

            Encoding.ASCII.GetBytes("TEST    = 1234/Comment").CopyTo(bSpan);

            var key = FitsKey.ParseRawData(bSpan);
            Assert.That(key is IFitsValue<int>);
            Assert.That((int)key.Value, Is.EqualTo(1234));
            Assert.That(key.Comment, Is.EqualTo(@" Comment"));
        }
    }
}
