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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fits_Cs
{
    public class DataBlob
    {
        public const int SizeInBytes = 2880;
        private readonly byte[] _data = new byte[SizeInBytes];
        public DataBlob()
        {
            
        }

        public async Task<bool> TryInitializeAsync(Stream stream, CancellationToken token = default)
        {
            if (stream?.CanRead != true)
                return false;

            var n = await stream.ReadAsync(_data, 0, SizeInBytes, token);

            if (n == SizeInBytes)
                return true;

            Array.Clear(_data, 0, _data.Length);
           
           return false;
        }

        public bool TryInitialize(ReadOnlySpan<byte> span)
        {
            return span.Length == SizeInBytes && span.TryCopyTo(_data.AsSpan());
        }

        public bool TryInitialize(ReadOnlyMemory<byte> memory)
            => TryInitialize(memory.Span);
        

        public bool TryInitialize(byte[] data)
        {
            if (data.Length != SizeInBytes)
                return false;
            Buffer.BlockCopy(data, 0, _data, 0, SizeInBytes);
            return true;
        }
        
    }
}
