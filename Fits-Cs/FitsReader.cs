﻿//     MIT License
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace FitsCs
{
    public class FitsReader : IDisposable, IAsyncDisposable
    {
        // 16 * 2880 bytes is ~ 45 KB
        // It allows to process up to 16 Fits IDUs at once
        public const int DefaultBufferSize = 16 * DataBlob.SizeInBytes;
        private readonly byte[] _buffer;
        private volatile int _nReadBytes;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);


        private readonly Stream _stream;
        private readonly bool _leaveOpen;


        private Span<byte> Span => _buffer;

        [PublicAPI]
        public FitsReader(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _buffer = new byte[DefaultBufferSize];
        }
        
        [PublicAPI]
        public FitsReader(
            Stream stream, 
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


        protected virtual void ReturnToBuffer(ReadOnlySpan<byte> data)
        {
            if(_buffer is null)
                throw new NullReferenceException(SR.UnexpectedNullRef);

            if (!data.TryCopyTo(Span.Slice(_nReadBytes)))
                throw new InvalidOperationException(SR.InvalidOperation);

            _nReadBytes += data.Length;
        }

        // TODO : Think of concurrency
        protected virtual void CompactBuffer(int n = 0)
        {
            if (_nReadBytes <= 0 || n < 0)
                return;

            n = n == 0 ? _nReadBytes : Math.Min(n, _nReadBytes);
            
            if(n < Span.Length)
                Span.Slice(n).CopyTo(Span);
            _nReadBytes -= n;
            
            Span.Slice(_nReadBytes).Fill(0);
        }

        protected virtual async ValueTask<int> ReadIntoBuffer(int start, int length, CancellationToken token, bool @lock)
        {
            if (@lock)
                // Synchronizing read access
                await _semaphore.WaitAsync(token);

            try
            {
                var n = await _stream.ReadAsync(_buffer, start, length, token);
                Interlocked.Add(ref _nReadBytes, n);
                return n;
            }
            finally
            {
                if (@lock)
                    _semaphore.Release();
            }
        }

        protected virtual async ValueTask<bool> ReadInnerAsync(DataBlob blob, CancellationToken token, bool @lock)
        {
            if (blob is null)
                throw new ArgumentException(SR.NullArgument, nameof(blob));
            
            if(@lock)
            // Synchronizing read access
                await _semaphore.WaitAsync(token);

            try
            {
                if (_nReadBytes < DataBlob.SizeInBytes)
                {
                    //var n = await _stream.ReadAsync(_buffer, _nReadBytes, _buffer.Length - _nReadBytes, token);
                    await ReadIntoBuffer(_nReadBytes, _buffer.Length - _nReadBytes, token, false);

                    if (_nReadBytes < DataBlob.SizeInBytes)
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
                    _semaphore.Release();
            }
        }

        protected virtual async ValueTask<int> FillDataAsync(
            Block block, CancellationToken token, bool @lock)
        {
            if (block is null)
                throw new ArgumentNullException(nameof(block), SR.NullArgument);
            if (@lock)
                // Synchronizing read access
                await _semaphore.WaitAsync(token);
            try
            {
                var len = block.RawData.Length;
                var alignedLen = (int)(Math.Ceiling(1.0 * block.RawData.Length / DataBlob.SizeInBytes) * DataBlob.SizeInBytes);
                if (alignedLen <= _nReadBytes)
                {
                   
                    if (Span.Slice(0, len).TryCopyTo(block.RawData))
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
                    if (_nReadBytes > 0)
                    {
                        if (Span.Slice(0, _nReadBytes).TryCopyTo(block.RawData))
                        {
                            count += _nReadBytes;
                            CompactBuffer();
                        }
                    }

                    var nReads = Math.Ceiling(1.0 * (alignedLen - count) / _buffer.Length);
                    for (var i = 0; i < nReads - 1; i++)
                    {
                        if (await ReadIntoBuffer(0, _buffer.Length, token, false) != _buffer.Length)
                            return -1;

                        if (!Span.TryCopyTo(block.RawData.Slice(count)))
                            return -1;
                        count += _buffer.Length;
                        CompactBuffer();
                    }

                    if (await ReadIntoBuffer(0, _buffer.Length, token, false) < (alignedLen - count)
                        || !Span.Slice(0, len - count).TryCopyTo(block.RawData.Slice(count)))
                        return -1;
                    
                    CompactBuffer(alignedLen - count);
                    count += len - count;

                    return count;
                }
            }
            finally
            {
                if (@lock)
                    _semaphore.Release();
            }
        }

        [PublicAPI]
        [ItemCanBeNull]
        public async ValueTask<DataBlob> ReadAsync(CancellationToken token = default)
        {
           var blob = new DataBlob();
           return await ReadInnerAsync(blob, token, true)
               ? blob
               : null;
        }

        [PublicAPI]
        public ValueTask<bool> ReadAsync(
            [NotNull] DataBlob blob,
            CancellationToken token = default)
        {
            return ReadInnerAsync(blob, token, true);
        }

        [PublicAPI]
        public async ValueTask<Block> ReadBlockAsync(CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token);
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

                var block = Block.Create(desc);
                block.Keys.AddRange(keys);
                var nBytesFilled = await FillDataAsync(block, token, false);

                if (nBytesFilled != block.DataSizeInBytes())
                    return null;//throw new IOException(SR.IOReadFailure);

                block.FlipEndianessIfNecessary();

                return block;
            }
            finally
            {
                _semaphore.Release();
            }
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
