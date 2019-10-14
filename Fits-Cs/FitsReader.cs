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
using JetBrains.Annotations;

namespace FitsCs
{
    public class FitsReader : IDisposable
    {
        // 16 * 2880 bytes is ~ 45 KB
        // It allows to process up to 16 Fits IDUs at once
        public const int DefaultBufferSize = 16 * DataBlob.SizeInBytes;
        private readonly byte[] _buffer;
        private int _nReadBytes;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);


        private readonly Stream _stream;
        private readonly bool _leaveOpen;


        private Span<byte> Span => _buffer;

        [PublicAPI]
        public FitsReader([NotNull] Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _buffer = new byte[DefaultBufferSize];
        }
        
        [PublicAPI]
        public FitsReader(
            [NotNull] Stream stream, 
            int bufferSize)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            var allowedBufferSize = bufferSize <= 0 || bufferSize < DataBlob.SizeInBytes
                ? DefaultBufferSize
                : bufferSize;

            _buffer = new byte[allowedBufferSize];
        }

        [PublicAPI]
        public FitsReader(
            [NotNull] Stream stream, 
            int bufferSize, bool leaveOpen)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            var allowedBufferSize = bufferSize <= 0 || bufferSize < DataBlob.SizeInBytes
                ? DefaultBufferSize
                : bufferSize;
            _leaveOpen = leaveOpen;

            _buffer = new byte[allowedBufferSize];
        }

        [PublicAPI]
        [ItemCanBeNull]
        public async Task<DataBlob> ReadAsync(CancellationToken token = default)
        {
            // Synchronizing read access
            await _semaphore.WaitAsync(token);

            try
            {
                var n = await _stream.ReadAsync(_buffer, _nReadBytes, DataBlob.SizeInBytes - _nReadBytes, token);
                _nReadBytes += n;

                if (_nReadBytes < DataBlob.SizeInBytes)
                    return null;
                
                var blob = new DataBlob();

                if (!blob.TryInitialize(Span.Slice(0, DataBlob.SizeInBytes)))
                    return null;

                Span.Slice(0, DataBlob.SizeInBytes).Fill(0);
                _nReadBytes = 0;
                
                return blob;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        [PublicAPI]
        public async Task<bool> ReadAsync(
            [NotNull] DataBlob blob,
            CancellationToken token = default)
        {
            if(blob is null)
                throw new ArgumentException(SR.NullArgument, nameof(blob));
            // Synchronizing read access
            await _semaphore.WaitAsync(token);

            try
            {
                if (_nReadBytes < DataBlob.SizeInBytes)
                {
                    var n = await _stream.ReadAsync(_buffer, _nReadBytes, DataBlob.SizeInBytes - _nReadBytes, token);
                    _nReadBytes += n;

                    if (_nReadBytes < DataBlob.SizeInBytes)
                        return false;
                }

                if(blob.IsInitialized)
                    blob.Reset();
                
                if (!blob.TryInitialize(Span.Slice(0, DataBlob.SizeInBytes)))
                    return false;

                if (Span.Length > DataBlob.SizeInBytes) 
                    Span.Slice(DataBlob.SizeInBytes).CopyTo(Span);

                _nReadBytes -= DataBlob.SizeInBytes;

                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        [PublicAPI]
        [ItemCanBeNull]
        public async Task<Block> ReadBlockAsync(CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token);
            try
            {
                var blob = new DataBlob();

                var n = await _stream.ReadAsync(_buffer, _nReadBytes, DataBlob.SizeInBytes - _nReadBytes, token);
                _nReadBytes += n;

                if (_nReadBytes < DataBlob.SizeInBytes)
                    return null;


                if (!blob.TryInitialize(Span.Slice(0, DataBlob.SizeInBytes)))
                    return null;


                Span.Slice(0, DataBlob.SizeInBytes).Fill(0);
                _nReadBytes = 0;

                


                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore.Dispose();
                if (!_leaveOpen)
                    _stream?.Dispose();
            }
        }

        [PublicAPI]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
