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
        private byte[] _data;
        public bool IsInitialized { get; private set; } = false;

        public ReadOnlySpan<byte> Data =>
            IsInitialized
                ? _data ?? ReadOnlySpan<byte>.Empty
                : ReadOnlySpan<byte>.Empty;

        public bool TryInitialize(ReadOnlySpan<byte> span)
        {
            if (IsInitialized || span.Length > SizeInBytes)
                return false;

            if(_data is null)
                _data = new byte[SizeInBytes];

            span.CopyTo(_data);
            IsInitialized = true;

            return true;
        }

        public bool TryInitialize(ReadOnlyMemory<byte> memory)
            => TryInitialize(memory.Span);
        
        public bool TryInitialize(byte[] data)
        {
            if (IsInitialized)
                return false;

            if (data.Length <= SizeInBytes)
                return false;

            if(_data is null)
                _data = new byte[SizeInBytes];
            Buffer.BlockCopy(data, 0, _data, 0, SizeInBytes);
            IsInitialized = true;
            return true;
        }

        public void Reset()
        {
            IsInitialized = false;
        }
    }
}
