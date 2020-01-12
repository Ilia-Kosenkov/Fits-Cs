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

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FitsCs
{
    public class FitsReader : BufferedStreamManager
    {


        public FitsReader(Stream stream) : base(stream, DefaultBufferSize, false)
        {
        }

        public FitsReader(
            Stream stream,
            int bufferSize)
            : base(stream, bufferSize, false)
        {
        }

        public FitsReader(
            Stream stream,
            int bufferSize, bool leaveOpen)
            : base(stream, bufferSize, leaveOpen)
        {
        }

        protected virtual void ReturnToBuffer(ReadOnlySpan<byte> data)
        {
            if(Buffer is null)
                throw new NullReferenceException(SR.UnexpectedNullRef);

            if (!data.TryCopyTo(Span.Slice(NBytesAvailable)))
                throw new InvalidOperationException(SR.InvalidOperation);

            NBytesAvailable += data.Length;
        }

        protected virtual async ValueTask<int> ReadIntoBuffer(int start, int length, CancellationToken token, bool @lock)
        {
            if (@lock)
                // Synchronizing read access
                await Semaphore.WaitAsync(token);

            try
            {
                var n = await Stream.ReadAsync(Buffer, start, length, token);
                Interlocked.Add(ref NBytesAvailable, n);
                return n;
            }
            finally
            {
                if (@lock)
                    Semaphore.Release();
            }
        }

        protected virtual async ValueTask<bool> ReadInnerAsync(DataBlob blob, CancellationToken token, bool @lock)
        {
            if (blob is null)
                throw new ArgumentException(SR.NullArgument, nameof(blob));
            
            if(@lock)
            // Synchronizing read access
                await Semaphore.WaitAsync(token);

            try
            {
                if (NBytesAvailable < DataBlob.SizeInBytes)
                {
                    //var n = await _stream.ReadAsync(_buffer, _nAvailableBytes, _buffer.Length - _nAvailableBytes, token);
                    await ReadIntoBuffer(NBytesAvailable, Buffer.Length - NBytesAvailable, token, false);

                    if (NBytesAvailable < DataBlob.SizeInBytes)
                        return false;
                }

                if (blob.IsInitialized)
                    blob.Reset();

                if (!blob.TryInitialize(Span.Slice(0, DataBlob.SizeInBytes)))
                    return false;
                CompactBuffer(DataBlob.SizeInBytes);

                return true;
            }
            finally
            {
                if(@lock)
                    Semaphore.Release();
            }
        }

        protected virtual async ValueTask<int> FillDataAsync(
            Block block, CancellationToken token, bool @lock)
        {
            if (block is null)
                throw new ArgumentNullException(nameof(block), SR.NullArgument);

            if (block.RawDataInternal.IsEmpty)
                return default;

            if (@lock)
                // Synchronizing read access
                await Semaphore.WaitAsync(token);
            try
            {
                var len = block.RawDataInternal.Length;
                var alignedLen = (int)(Math.Ceiling(1.0 * block.RawDataInternal.Length / DataBlob.SizeInBytes) * DataBlob.SizeInBytes);
                if (alignedLen <= NBytesAvailable)
                {
                   
                    if (Span.Slice(0, len).TryCopyTo(block.RawDataInternal))
                    {
                        // Compacting accounting for alignment
                        CompactBuffer(alignedLen);
                        return len;
                    }
                    else
                        return -1;
                }
                else
                {
                    var count = 0;
                    if (NBytesAvailable > 0)
                    {
                        if (Span.Slice(0, NBytesAvailable).TryCopyTo(block.RawDataInternal))
                        {
                            count += NBytesAvailable;
                            CompactBuffer();
                        }
                    }

                    var nReads = Math.Ceiling(1.0 * (alignedLen - count) / Buffer.Length);
                    for (var i = 0; i < nReads - 1; i++)
                    {
                        if (await ReadIntoBuffer(0, Buffer.Length, token, false) != Buffer.Length)
                            return -1;

                        if (!Span.TryCopyTo(block.RawDataInternal.Slice(count)))
                            return -1;
                        count += Buffer.Length;
                        CompactBuffer();
                    }

                    if (await ReadIntoBuffer(0, Buffer.Length, token, false) < (alignedLen - count)
                        || !Span.Slice(0, len - count).TryCopyTo(block.RawDataInternal.Slice(count)))
                        return -1;
                    
                    CompactBuffer(alignedLen - count);
                    count += len - count;

                    return count;
                }
            }
            finally
            {
                if (@lock)
                    Semaphore.Release();
            }
        }

        public async ValueTask<DataBlob?> ReadAsync(CancellationToken token = default)
        {
           var blob = new DataBlob();
           return await ReadInnerAsync(blob, token, true)
               ? blob
               : null;
        }

        public ValueTask<bool> ReadAsync(
            DataBlob blob,
            CancellationToken token = default)
        {
            return ReadInnerAsync(blob, token, true);
        }

        public async ValueTask<Block?> ReadBlockAsync(CancellationToken token = default)
        {
            await Semaphore.WaitAsync(token);
            try
            {
                var keys = new List<IFitsValue>(4 * DataBlob.SizeInBytes / FitsKey.EntrySizeInBytes);
                var blob = new DataBlob();
                var @continue = true;
                while (@continue)
                {
                    if (!await ReadInnerAsync(blob, token, false))
                        return null;//throw new InvalidOperationException(SR.InvalidOperation);

                    if (blob.GetContentType() == BlobType.FitsHeader &&
                        blob.AsKeyCollection() is var tempKeyCollection)
                    {
                        keys.AddRange(tempKeyCollection);
                        @continue = !tempKeyCollection.IsEnd();
                    }
                    else
                    {
                        ReturnToBuffer(blob.Data);
                        @continue = false;
                    }
                }

                // TODO: catch specific exception and rethrow 
                var desc = new Descriptor(keys);

                var block = Block.Create(desc, keys);
                var nBytesFilled = await FillDataAsync(block, token, false);

                if (nBytesFilled != block.DataSizeInBytes())
                    return null;//throw new IOException(SR.IOReadFailure);

                block.FlipEndianessIfNecessary();

                return block;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        public async IAsyncEnumerable<Block> EnumerateBlocksAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            Block? block;
            while ((block = await ReadBlockAsync(token)) is {})
                yield return block;
        }
    }
}
