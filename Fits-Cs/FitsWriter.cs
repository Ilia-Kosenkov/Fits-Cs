#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FitsCs
{
    public class FitsWriter : BufferedStreamManager, IDisposable, IAsyncDisposable
    {
        // 16 * 2880 bytes is ~ 45 KB
        // It allows to process up to 16 Fits IDUs at once

        public FitsWriter(Stream stream) : base(stream, DefaultBufferSize, false)
        {
        }

        public FitsWriter(
            Stream stream,
            int bufferSize) : base(stream, bufferSize, false)
        { }

        public FitsWriter(
            Stream stream,
            int bufferSize, bool leaveOpen) : base(stream, bufferSize, leaveOpen)
        {
        }

        public virtual ValueTask WriteAsync(DataBlob blob, CancellationToken token = default) 
            => throw  new NotImplementedException(SR.MethodNotImplemented);

        public virtual ValueTask WriteBlockAsync(Block block, CancellationToken token = default)
            => throw new NotImplementedException(SR.MethodNotImplemented);

        public virtual ValueTask Flush(CancellationToken token = default)
            => FlushBufferAsync(token, true);

        protected virtual ValueTask WriteInnerAsync(DataBlob blob, CancellationToken token, bool @lock)
        {
            

            throw new NotImplementedException(SR.MethodNotImplemented);
        }

        protected virtual async ValueTask FlushBufferAsync(CancellationToken token, bool @lock)
        {
            if (NBytesAvailable <= 0)
                return;

            if (@lock)
                await Semaphore.WaitAsync(token);
            try
            {
                await Stream.WriteAsync(Buffer, 0, NBytesAvailable, token);
                CompactBuffer(NBytesAvailable);
            }
            finally
            {
                if (@lock)
                    Semaphore.Release();
            }
        }

        protected virtual void FlushBuffer(bool @lock)
        {
            if (@lock)
                Semaphore.Wait();
            
            if(NBytesAvailable <= 0)
                return;

            try
            {
                Stream.Write(Buffer, 0, NBytesAvailable);
                CompactBuffer(NBytesAvailable);
            }
            finally
            {
                if (@lock)
                    Semaphore.Release();
            }
        }

        protected override void Dispose(bool disposing)
        { 
            if (!disposing) return;

            if (NBytesAvailable > 0)
                FlushBuffer(false);

            base.Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (NBytesAvailable > 0)
                await FlushBufferAsync(default, false);

            Semaphore.Dispose();
            if (LeaveOpen)
                Stream?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
