using System;

namespace FitsCs
{
    public class BufferManagerBase
    {
        // Cannot fully abstract these,
        // Streams do not have Span overloads in .NS 2.0
        protected byte[] Buffer;
        protected volatile int NBytesAvailable;
        public const int DefaultBufferSize = 16 * DataBlob.SizeInBytes;
        protected Span<byte> Span => Buffer;

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
    }
}