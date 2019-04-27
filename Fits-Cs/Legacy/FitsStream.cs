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


namespace FITS_CS
{
    public class FitsStream : Stream, IDisposable
    {
        private readonly Stream _baseStream;

        public bool IsDisposed { get; private set; }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override void Flush()
            => _baseStream.Flush();


        public override int Read(byte[] buffer, int offset, int count)
            => _baseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => _baseStream.Seek(offset, origin);

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => _baseStream.Write(buffer, offset, count);

        public void WriteUnit(FitsUnit unit)
        {
            Write(unit._data, 0, FitsUnit.UnitSizeInBytes);
            Flush();
        }

        public async Task WriteUnitAsync(FitsUnit unit, CancellationToken token = default)
        {
            await WriteAsync(unit._data, 0, unit._data.Length, token);
            await FlushAsync(token);
        }

        public FitsStream(Stream str)
            => _baseStream = str ?? throw new ArgumentNullException($"{nameof(str)} is null");

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
                if (_baseStream != null)
                {
                    _baseStream.Dispose();
                    IsDisposed = true;
                }

        }

        public override void Close() => Dispose();

        public FitsUnit ReadUnit()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Stream is already disposed.");
            if (!CanRead)
                throw new NotSupportedException("Stream does not support reading.");

            var buffer = new byte[FitsUnit.UnitSizeInBytes];

            if (CanSeek && Position + FitsUnit.UnitSizeInBytes > Length)
                throw new ArgumentException("Stream ended");
            _baseStream.Read(buffer, 0, FitsUnit.UnitSizeInBytes);
            return new FitsUnit(buffer);

        }

        public bool TryReadUnit(out FitsUnit unit)
        {
            unit = null;
            try
            {
                unit = ReadUnit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
