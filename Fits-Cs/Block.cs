﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        public int ParamCount { get; }
        public int GroupCount { get; }

        public abstract Span<byte> RawData { get; }
        
        public abstract bool IsPrimary { get; }

        public abstract void FlipEndianessIfNecessary();

        public long DataCount() => GroupCount * (Dimensions.Aggregate<long, int>(1, (prod, n) => prod * n) + ParamCount);
        public long DataSizeInBytes() => DataCount() * ItemSizeInBytes;

        protected internal Block(Descriptor desc)
        {
            
            if (desc.IsEmpty)
                throw new ArgumentException(SR.InvalidArgument, nameof(desc));
            Keys = new List<IFitsValue>(desc.Nkeys);
            Dimensions = desc.Dimensions;
            DataType = desc.DataType;
            ItemSizeInBytes = desc.ItemSizeInBytes;
            ParamCount = desc.ParamCount;
            GroupCount = desc.GroupCount;
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
                    return new PrimaryBlock<short>(desc);
                if(desc.DataType == typeof(int))
                    return new PrimaryBlock<int>(desc);

                throw new ArgumentException(SR.InvalidArgument, nameof(desc));
            }

            if (desc.DataType == typeof(float))
                return new ExtensionBlock<float>(desc);
            if (desc.DataType == typeof(double))
                return new ExtensionBlock<double>(desc);
            if (desc.DataType == typeof(byte))
                return new ExtensionBlock<byte>(desc);
            if (desc.DataType == typeof(short))
                return new ExtensionBlock<short>(desc);
            if (desc.DataType == typeof(int))
                return new ExtensionBlock<int>(desc);

            throw new ArgumentException(SR.InvalidArgument, nameof(desc));

        }
    }

    public abstract class Block<T> : Block where T : unmanaged
    {
        private bool _hasCorrectEndianess;
        // ReSharper disable once InconsistentNaming
        private protected T[] _data;
        public Span<T> Data => _data ?? Span<T>.Empty;
        public override Span<byte> RawData => MemoryMarshal.AsBytes(Data);

        public override void FlipEndianessIfNecessary()
        {
            if (!_hasCorrectEndianess)
            {
                var span = RawData;
                if (ItemSizeInBytes == 2)
                {
                    for (var i = 0; i < _data.Length; i++)
                    {
                        var offset = 2 * i;
                        var temp = span[offset];
                        span[offset] = span[offset + 1];
                        span[offset + 1] = temp;
                    }
                }
                else if (ItemSizeInBytes == 4)
                {
                    for (var i = 0; i < _data.Length; i++)
                    {
                        var offset = 4 * i;
                        
                        var temp = span[offset];
                        span[offset] = span[offset + 3];
                        span[offset + 3] = temp;

                        temp = span[offset + 1];
                        span[offset + 1] = span[offset + 2];
                        span[offset + 2] = temp;
                    }
                }
                else if (ItemSizeInBytes == 8)
                {
                    for (var i = 0; i < _data.Length; i++)
                    {
                        var offset = 8 * i;

                        var temp = span[offset];
                        span[offset] = span[offset + 7];
                        span[offset + 7] = temp;

                        temp = span[offset + 1];
                        span[offset + 1] = span[offset + 6];
                        span[offset + 6] = temp;

                        temp = span[offset + 2];
                        span[offset + 2] = span[offset + 5];
                        span[offset + 5] = temp;

                        temp = span[offset + 3];
                        span[offset + 3] = span[offset + 4];
                        span[offset + 4] = temp;
                    }
                }

                _hasCorrectEndianess = true;
            }
        }

        protected internal Block(Descriptor desc) : base(desc)
        {
            AllowedTypes.ValidateDataType<T>();
            if (desc.IsEmpty)
                throw new ArgumentException(SR.InvalidArgument, nameof(desc));

            _data = new T[desc.GetFullSize()];
            _hasCorrectEndianess = !BitConverter.IsLittleEndian;
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