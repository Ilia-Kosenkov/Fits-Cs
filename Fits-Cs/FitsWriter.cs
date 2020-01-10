using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FitsCs
{
    public class FitsWriter : IDisposable, IAsyncDisposable
    {
        // 16 * 2880 bytes is ~ 45 KB
        // It allows to process up to 16 Fits IDUs at once
        public const int DefaultBufferSize = 16 * DataBlob.SizeInBytes;
        private readonly byte[] _buffer;

        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private int _nWrittenBytes;

        private Span<byte> Span => _buffer;

        public FitsWriter(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _buffer = new byte[DefaultBufferSize];
        }

        public FitsWriter(
            Stream stream,
            int bufferSize)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            var allowedBufferSize = bufferSize <= 0 || bufferSize < DataBlob.SizeInBytes
                ? DefaultBufferSize
                : bufferSize;

            _buffer = new byte[allowedBufferSize];
        }

        public FitsWriter(
            Stream stream,
            int bufferSize, bool leaveOpen)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            var allowedBufferSize = bufferSize <= 0 || bufferSize < DataBlob.SizeInBytes
                ? DefaultBufferSize
                : bufferSize;
            _leaveOpen = leaveOpen;

            _buffer = new byte[allowedBufferSize];
        }

        public virtual ValueTask WriteAsync(DataBlob blob, CancellationToken token = default) 
            => throw  new NotImplementedException(SR.MethodNotImplemented);

        public virtual ValueTask WriteBlockAsync(Block block, CancellationToken token = default)
            => throw new NotImplementedException(SR.MethodNotImplemented);

        protected virtual ValueTask WriteInnerAsync(DataBlob blob, CancellationToken token, bool @lock)
            => throw new NotImplementedException(SR.MethodNotImplemented);

        protected virtual async ValueTask FlushBuffer(int start, int length, CancellationToken token, bool @lock)
        {
            if (@lock)
                await _semaphore.WaitAsync(token);
            try
            {
                await _stream.WriteAsync(_buffer, 0, _nWrittenBytes, token);
                CompactBuffer(_nWrittenBytes);

            }
            finally
            {
                if (@lock)
                    _semaphore.Release();
            }
        }

        protected virtual void CompactBuffer(int n = 0)
        {
            throw new NotImplementedException(SR.MethodNotImplemented);
        }

        protected virtual void Dispose(bool disposing)
        { 
            if (!disposing) return;

            _semaphore.Dispose();
            if (!_leaveOpen)
                _stream?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                Dispose();
                return default;
            }
            catch (Exception e)
            {
                return new ValueTask(Task.FromException(e));
            }
        }
    }
}
