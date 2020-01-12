#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FitsCs
{
    public class FitsWriter : BufferedStreamManager
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

        public virtual ValueTask<bool> WriteAsync(DataBlob blob, CancellationToken token = default) 
            => WriteInnerAsync(blob, token, true);

        public virtual async ValueTask WriteBlockAsync(Block block, CancellationToken token = default)
        {
            if (block is null)
                throw new ArgumentNullException(nameof(block), SR.NullArgument);

            await Semaphore.WaitAsync(token);

            try
            {
                foreach (var blob in block.AsBlobStream()) 
                    await WriteInnerAsync(blob, token, false);
            }
            finally
            {
                Semaphore.Release();
            }
        }

        public virtual ValueTask Flush(CancellationToken token = default)
            => FlushBufferAsync(token, true);

        protected virtual async ValueTask<bool> WriteInnerAsync(DataBlob blob, CancellationToken token, bool @lock)
        {
            if (blob?.IsInitialized != true)
                return false;

            if (@lock)
                await Semaphore.WaitAsync(token);
            try
            {
                if (Span.Length - NBytesAvailable < blob.Data.Length)
                    // Not enough space to write one blob
                    await FlushBufferAsync(token, false);

                // Buffer is always larger than a blob's size
                return blob.Data.TryCopyTo(Span.Slice(NBytesAvailable));
            }
            finally
            {
                Interlocked.Add(ref NBytesAvailable, DataBlob.SizeInBytes);
                if (@lock)
                    Semaphore.Release();
            }
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

        public override async ValueTask DisposeAsync()
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
