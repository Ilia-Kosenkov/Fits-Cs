using System;
using System.Runtime.InteropServices;
using FitsCs;
using NUnit.Framework;

namespace Tests
{
    public class BlockTests
    {
        private int[] _data;
        
        [SetUp]
        public void Setup()
        {
            _data = new int [DataBlob.SizeInBytes / sizeof(int)];
            for (var i = 0; i < _data.Length; i++)
                _data[i] = i;
        }

        [Test]
        public void Test_ByteFlipping()
        {
            var desc = new Descriptor(32, 0, new[] { 20, 36 });

            var block = Block.Create(desc) as Block<int> ?? throw new Exception();
            MemoryMarshal.AsBytes<int>(_data).CopyTo(block.RawData);
            Assert.IsTrue(block.Data.SequenceEqual(_data));

            block.FlipEndianessIfNecessary();
            Assert.IsFalse(block.Data.SequenceEqual(_data));


            var block2 = Block.Create(desc) as Block<int> ?? throw new Exception();
            block.RawData.CopyTo(block2.RawData);
            Assert.IsTrue(block.Data.SequenceEqual(block2.Data));

            block2.FlipEndianessIfNecessary();
            Assert.IsFalse(block.Data.SequenceEqual(block2.Data));
            Assert.IsTrue(block2.Data.SequenceEqual(_data));
        }
    }
}