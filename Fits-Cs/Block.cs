using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace FitsCs
{
    public abstract class Block
    {
        public List<IFitsValue> Keys { get; }
    
        public ImmutableArray<int> Dimensions { get; }
        public Type DataType { get; }
        public byte ItemSizeInBytes { get; }

        public abstract Span<byte> RawData { get; }
        
        public abstract bool IsPrimary { get; }

        protected internal Block(Descriptor desc)
        {
            
            if (desc.IsEmpty)
                throw new ArgumentException(SR.InvalidArgument, nameof(desc));
            Keys = new List<IFitsValue>(desc.Nkeys);
            Dimensions = desc.Dimensions;
            DataType = desc.DataType;
            ItemSizeInBytes = desc.ItemSizeInBytes;
        }

        public static Type ConvertBitPixToType(int bitpix)
        {
            switch (bitpix)
            {
                case 8:
                    return typeof(byte);
                case 16:
                    return typeof(short);
                case 32:
                    return typeof(int);
                case -32:
                    return typeof(float);
                case -64:
                    return typeof(double);
                default:
                    return null;
            }
        }

        [NotNull]
        public static Block Create(Descriptor desc)
        {
            AllowedTypes.ValidateDataType(desc.DataType);

            if (desc.IsPrimary)
            {
                if(desc.DataType == typeof(float))
                    return new PrimaryBlock<float>(desc);
                if(desc.DataType == typeof(double))
                    return new PrimaryBlock<double>(desc);
                if(desc.DataType == typeof(byte))
                    return new PrimaryBlock<byte>(desc);
                if(desc.DataType == typeof(short))
                    return new PrimaryBlock<byte>(desc);
                if(desc.DataType == typeof(int))
                    return new PrimaryBlock<byte>(desc);

                throw new ArgumentException(SR.InvalidArgument, nameof(desc));
            }

            if (desc.DataType == typeof(float))
                return new ExtensionBlock<float>(desc);
            if (desc.DataType == typeof(double))
                return new ExtensionBlock<double>(desc);
            if (desc.DataType == typeof(byte))
                return new ExtensionBlock<byte>(desc);
            if (desc.DataType == typeof(short))
                return new ExtensionBlock<byte>(desc);
            if (desc.DataType == typeof(int))
                return new ExtensionBlock<byte>(desc);

            throw new ArgumentException(SR.InvalidArgument, nameof(desc));

        }
    }

    public abstract class Block<T> : Block where T : unmanaged
    {
        // ReSharper disable once InconsistentNaming
        private protected T[] _data;

        public Span<T> Data => _data ?? Span<T>.Empty;
        public override Span<byte> RawData => MemoryMarshal.AsBytes(Data);

        protected internal Block(Descriptor desc) : base(desc)
        {
            AllowedTypes.ValidateDataType<T>();
            if (desc.IsEmpty)
                throw new ArgumentException(SR.InvalidArgument, nameof(desc));

            _data = new T[desc.GetFullSize()];
        }
    }

    public sealed class PrimaryBlock<T> : Block<T> where T :unmanaged
    {
        public override bool IsPrimary => true;

        public PrimaryBlock(Descriptor desc) : base(desc)
        {
           
        }

    }

    public sealed class ExtensionBlock<T> : Block<T> where T : unmanaged
    {
        public override bool IsPrimary => false;

        public ExtensionBlock(Descriptor desc) : base(desc)
        {
        }
    }
}
