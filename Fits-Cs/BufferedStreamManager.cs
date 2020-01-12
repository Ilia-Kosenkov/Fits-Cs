#nullable enable

using System;
using System.IO;
using System.Threading;

namespace FitsCs
{
    public class BufferedStreamManager
    {
        // Cannot fully abstract these,
        // Streams do not have Span overloads in .NS 2.0
        public const int DefaultBufferSize = 16 * DataBlob.SizeInBytes;
        protected byte[] Buffer;
        protected volatile int NBytesAvailable;
        protected Span<byte> Span => Buffer;

        // 16 * 2880 bytes is ~ 45 KB
        // It allows to process up to 16 Fits IDUs at once
        protected readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        protected readonly Stream Stream;
        protected readonly bool LeaveOpen;

        protected virtual void CompactBuffer(int n = 0)
        {
            if (NBytesAvailable <= 0 || n < 0)
                return;

            n = n == 0 ? NBytesAvailable : Math.Min(n, NBytesAvailable);
            
            if(n < Span.Length)
                Span.Slice(n).CopyTo(Span);
            NBytesAvailable -= n;
            
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
                Stream?.Dispose();
        }
    }
}