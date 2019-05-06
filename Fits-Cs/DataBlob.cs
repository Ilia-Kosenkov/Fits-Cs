//     MIT License
//     
//     Copyright(c) 2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;

namespace FitsCs
{
    public class DataBlob
    {
        public const int KeysPerBlob = SizeInBytes / FitsKey.EntrySize;
        public const int SizeInBytes = 2880;
        private readonly byte[] _data;
        public bool IsInitialized { get; private set; }
        public ReadOnlyMemory<byte> Memory { get; }

        public DataBlob()
        {
            _data = new byte[SizeInBytes];
            Memory = new ReadOnlyMemory<byte>(_data);
        }

        public bool TryInitialize(ReadOnlySpan<byte> span)
        {
            var isOk = span.Length == SizeInBytes && span.TryCopyTo(_data.AsSpan());
            IsInitialized = isOk;
            return isOk;
        }

        public bool TryInitialize(ReadOnlyMemory<byte> memory)
            => TryInitialize(memory.Span);
        
        public bool TryInitialize(byte[] data)
        {
            IsInitialized = false;
            if (data.Length <= SizeInBytes)
                return false;
            Buffer.BlockCopy(data, 0, _data, 0, SizeInBytes);
            IsInitialized = true;
            return true;
        }

        public bool TestIsKeywordsWeak()
        {
            var isFirstKey = FitsKey.IsValidKeyName(Memory.Span.Slice(0, FitsKey.NameSize));
            if (!isFirstKey)
                return false;

            var lastKeySpan = Memory.Span.Slice(Memory.Length - FitsKey.EntrySize, FitsKey.NameSize);

            return FitsKey.IsValidKeyName(lastKeySpan);
        }

        public bool TestIsKeywordsStrong()
        {
            var isFirstKey = FitsKey.IsValidKeyName(Memory.Span.Slice(0, FitsKey.NameSize));
            if (!isFirstKey)
                return false;

            for (var i = 1; i < KeysPerBlob; i++)
                if (!FitsKey.IsValidKeyName(Memory.Span.Slice(i * FitsKey.EntrySize, FitsKey.NameSize)))
                    return false;

            return true;
        }
    }
}
