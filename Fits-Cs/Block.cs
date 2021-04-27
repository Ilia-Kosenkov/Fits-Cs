#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace FitsCs
{
    public abstract class Block
    {
        public delegate void Initializer<T>(Span<T> buffer) where T : unmanaged;

        public ImmutableList<IFitsValue> Keys { get; }
    
        public Descriptor Descriptor { get; }

        public abstract ReadOnlySpan<byte> RawData { get; }

        internal abstract Span<byte> RawDataInternal { get; }

        internal void FlipEndianess()
        {
            if (!BitConverter.IsLittleEndian)
                return;
            Span<byte> span = RawDataInternal;
            var itemSizeInBytes = Descriptor.ItemSizeInBytes;
            span.FlipEndianess(itemSizeInBytes);
        }

        public long DataCount() => Descriptor.GetFullSize();
        public long DataSizeInBytes() => DataCount() * Descriptor.ItemSizeInBytes;

        public abstract IEnumerable<DataBlob> AsBlobStream();

        protected internal Block(Descriptor desc, IEnumerable<IFitsValue>? keys)
        {
            if (desc.IsEmpty)
                throw new ArgumentException(SR.InvalidArgument, nameof(desc));
            Descriptor = desc;

            Keys = keys?.ToImmutableList() ?? ImmutableList<IFitsValue>.Empty;
        }

        public static Block Create(Descriptor desc, IEnumerable<IFitsValue>? keys)
        {
            AllowedTypes.ValidateDataType(desc.DataType);
            
            if(desc.DataType == typeof(float))
                return new Block<float>(desc, keys);
            if(desc.DataType == typeof(double))
                return new Block<double>(desc, keys);
            if(desc.DataType == typeof(byte))
                return new Block<byte>(desc, keys);
            if(desc.DataType == typeof(short))
                return new Block<short>(desc, keys);
            if(desc.DataType == typeof(int))
                return new Block<int>(desc, keys);
            if (desc.DataType == typeof(long))
                return new Block<long>(desc, keys);

            throw new ArgumentException(SR.InvalidArgument, nameof(desc));
        }

        public static Block Create(Descriptor desc, IEnumerable<IFitsValue>? keys, Initializer<byte> initializer)
        {
            AllowedTypes.ValidateDataType(desc.DataType);
            _ = initializer ?? throw new ArgumentNullException(nameof(initializer));

            if (desc.DataType == typeof(float))
                return new Block<float>(desc, keys, initializer);
            if (desc.DataType == typeof(double))
                return new Block<double>(desc, keys, initializer);
            if (desc.DataType == typeof(byte))
                return new Block<byte>(desc, keys, initializer);
            if (desc.DataType == typeof(short))
                return new Block<short>(desc, keys, initializer);
            if (desc.DataType == typeof(int))
                return new Block<int>(desc, keys, initializer);
            if (desc.DataType == typeof(long))
                return new Block<long>(desc, keys, initializer);

            throw new ArgumentException(SR.InvalidArgument, nameof(desc));
        }
    }

    public class Block<T> : Block where T : unmanaged
    {
        // ReSharper disable once InconsistentNaming
        private protected readonly T[] _data;
        internal override Span<byte> RawDataInternal => MemoryMarshal.AsBytes(_data.AsSpan());
        public ReadOnlySpan<T> Data => _data;
        public override ReadOnlySpan<byte> RawData => RawDataInternal;
        public override IEnumerable<DataBlob> AsBlobStream()
        {
            foreach (var b in DataBlob.AsBlobStream(Keys))
                yield return b;

            var n = DataSizeInBytes();
            if(n <= 0)
                yield break;

            var blob = new DataBlob();
            var offset = 0;
            var itemSizeInBytes = Descriptor.ItemSizeInBytes;

            while (offset < n)
            {
                blob.Reset();

                if (!blob.TryInitialize(
                    RawDataInternal.Slice(
                        offset,
                        (int) Math.Min(DataBlob.SizeInBytes, n - offset))))
                    throw new InvalidOperationException(SR.InvalidOperation);

                blob.FlipEndianess(itemSizeInBytes);
                offset += DataBlob.SizeInBytes;
                yield return blob;
            }
        }

        protected internal Block(Descriptor desc, IEnumerable<IFitsValue>? keys) : base(desc, keys)
        {
            AllowedTypes.ValidateDataType<T>();
           

            _data = new T[desc.GetFullSize()];
        }

        protected internal Block(
            Descriptor desc, 
            IEnumerable<IFitsValue>? keys,
            Initializer<T> initializer)
            : base(desc, keys)
        {
            AllowedTypes.ValidateDataType<T>();
            if (initializer is null)
                throw new ArgumentNullException(nameof(initializer), SR.NullArgument);
            
            _data = new T[desc.GetFullSize()];
            // Initializes data using action
            initializer(_data);
        }

        // This is for generic byte-initialization
        public Block(
            Descriptor desc,
            IEnumerable<IFitsValue>? keys,
            Initializer<byte> initializer)
            : base(desc, keys)
        {
            AllowedTypes.ValidateDataType<T>();
            if (initializer is null)
                throw new ArgumentNullException(nameof(initializer), SR.NullArgument);

            _data = new T[desc.GetFullSize()];
            // Initializes data using action
            initializer(MemoryMarshal.AsBytes(_data.AsSpan()));
        }


        // This is for public & external use; allows to write directly into the data array
        public static Block<T> CreateWithData(
            Descriptor desc,
            IEnumerable<IFitsValue>? keys,
            Initializer<T> initializer)
        {
            if (typeof(T) != desc.DataType)
                throw new ArgumentException(SR.InvalidArgument, nameof(desc));

            return new Block<T>(desc, keys, initializer);
        }
    }
}
