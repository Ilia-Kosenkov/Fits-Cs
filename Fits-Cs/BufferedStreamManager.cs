#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FitsCs
{
    public class BufferedStreamManager : IDisposable, IAsyncDisposable
    {
        // Cannot fully abstract these,
        // Streams do not have Span overloads in .NS 2.0
        protected const int DefaultBufferSize = 16 * DataBlob.SizeInBytes;
        protected readonly byte[] Buffer;
        protected volatile int NBytesAvailable;
        protected Span<byte> Span => Buffer;

        // 16 * 2880 bytes is ~ 45 KB
        // It allows to process up to 16 Fits IDUs at once
        protected readonly SemaphoreSlim Semaphore = new(1, 1);

        protected readonly Stream Stream;
        protected readonly bool LeaveOpen;

        protected void CompactBuffer(int n = 0)
        {
            if (NBytesAvailable <= 0 || n < 0)
                return;

            n = n == 0 ? NBytesAvailable : Math.Min(n, NBytesAvailable);

            if (n < Span.Length)
            {
                Span.Slice(n).CopyTo(Span);
            }

            Interlocked.Add(ref NBytesAvailable, -n);
            
            Span.Slice(NBytesAvailable).Fill(0);
        }

        protected BufferedStreamManager(Stream stream, int bufferSize, bool leaveOpen)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));

            var size = bufferSize <= 0 || bufferSize < DataBlob.SizeInBytes
                ? DefaultBufferSize
                : bufferSize;

            Buffer = new byte[size];

            LeaveOpen = leaveOpen;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Semaphore.Dispose();
            if (!LeaveOpen)
                Stream.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual ValueTask DisposeAsync()
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