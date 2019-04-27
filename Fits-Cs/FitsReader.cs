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
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FitsCs
{
    public class FitsReader : IDisposable
    {
        // 16 * 2880 bytes is ~ 45 KB
        // It allows to process up to 16 Fits IDUs at once
        public const int BufferSize = 16 * DataBlob.SizeInBytes;
        private readonly int _allowedBufferSize;
        private readonly int _blobsInBuffer;
        private byte[] _buffer;

        private readonly Stream _stream;
        private readonly bool _leaveOpen;

        public FitsReader(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _allowedBufferSize = BufferSize;
            _blobsInBuffer = _allowedBufferSize / DataBlob.SizeInBytes;
        }

        public FitsReader(Stream stream, int bufferSize)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _allowedBufferSize = bufferSize <= 0
                    ? 0
                    : DataBlob.SizeInBytes * Math.Max(1, bufferSize / DataBlob.SizeInBytes);
            _blobsInBuffer = _allowedBufferSize / DataBlob.SizeInBytes;
        }

        public FitsReader(Stream stream, int bufferSize, bool leaveOpen)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _allowedBufferSize = bufferSize <= 0
                ? 0
                : DataBlob.SizeInBytes * Math.Max(1, bufferSize / DataBlob.SizeInBytes);
            _blobsInBuffer = _allowedBufferSize / DataBlob.SizeInBytes;
            _leaveOpen = leaveOpen;
        }

        public async Task<DataBlob> ReadAsync(CancellationToken token = default)
        {
            var blob = new DataBlob();
            return await blob.TryInitializeAsync(_stream, token)
                ? blob
                : null;

        }

        public Task<ImmutableArray<DataBlob>> ReadBlockAsync(int n, CancellationToken token = default)
        {
            if(n <= 0)
                throw new ArgumentException(@"Should be positive.", nameof(n));
            int nBlobs;

            if (_stream.CanSeek)
            {
                // We can estimate exactly how much data there are to read
                var bytesLeft = _stream.Length - _stream.Position;
                if (bytesLeft < DataBlob.SizeInBytes)
                    return Task.FromResult(ImmutableArray<DataBlob>.Empty);

                nBlobs = (int)Math.Min(bytesLeft / DataBlob.SizeInBytes, n);


            }
            else
                nBlobs = n;

            return _blobsInBuffer == 0
                ? ReadBlockAsyncNoBuffer(nBlobs, token)
                : ReadBlockAsyncWithBuffer(nBlobs, token);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(!_leaveOpen)
                    _stream?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal async Task<ImmutableArray<DataBlob>> ReadBlockAsyncWithBuffer(int nBlobs, CancellationToken token)
        {
            if (_buffer is null)
                _buffer = new byte[_allowedBufferSize];
            var memory = new ReadOnlyMemory<byte>(_buffer);

            var builder = ImmutableArray.CreateBuilder<DataBlob>(nBlobs);
            var nReads = (int)Math.Ceiling(1.0 * nBlobs / _blobsInBuffer);

            for (var i = 0; i < nReads; i++)
            {
                var bytesToRead = i == nReads - 1
                    ? nBlobs * DataBlob.SizeInBytes - (nReads - 1) * _buffer.Length
                    : _buffer.Length;

                var nReadBytes = await _stream.ReadAsync(_buffer, 0, bytesToRead, token);
                if (nReadBytes % DataBlob.SizeInBytes != 0)
                    throw new IOException("Inconsistent Stream size.");

                if (nReadBytes == 0)
                    break;

                for (var j = 0; j < nReadBytes / DataBlob.SizeInBytes; j++)
                {
                    var blob = new DataBlob();
                    if (!blob.TryInitialize(memory.Slice(j * DataBlob.SizeInBytes, DataBlob.SizeInBytes)))
                        throw new InvalidOperationException("Failed to copy data.");
                    builder.Add(blob);
                }
            }

            return builder.ToImmutable();
        }

        internal async Task<ImmutableArray<DataBlob>> ReadBlockAsyncNoBuffer(int nBlobs, CancellationToken token)
        {
            var builder = ImmutableArray.CreateBuilder<DataBlob>(nBlobs);

            for (var i = 0; i < nBlobs; i++)
            {
                var blob = new DataBlob();
                if (!await blob.TryInitializeAsync(_stream, token))
                    break;
                builder.Add(blob);
            }

            return builder.ToImmutable();
        }
    }
}
