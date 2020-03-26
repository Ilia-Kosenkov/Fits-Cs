using System;
using System.Linq;
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
            var desc = new Descriptor(32, new[] { 20, 36 });

            var block = Block.Create(desc, Enumerable.Empty<IFitsValue>()) as Block<int> ?? throw new Exception();
            MemoryMarshal.AsBytes<int>(_data).CopyTo(block.RawDataInternal);
            Assert.IsTrue(block.Data.SequenceEqual(_data));

            block.FlipEndianess();
            Assert.IsFalse(block.Data.SequenceEqual(_data));


            var block2 = Block.Create(desc, Enumerable.Empty<IFitsValue>()) as Block<int> ?? throw new Exception();
            block.RawDataInternal.CopyTo(block2.RawDataInternal);
            Assert.IsTrue(block.Data.SequenceEqual(block2.Data));

            block2.FlipEndianess();
            Assert.IsFalse(block.Data.SequenceEqual(block2.Data));
            Assert.IsTrue(block2.Data.SequenceEqual(_data));
        }
    }
}