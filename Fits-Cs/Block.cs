#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FitsCs
{
    public abstract class Block
    {
        public List<IFitsValue> Keys { get; }
    
        public Descriptor Descriptor { get; }

        public abstract Span<byte> RawData { get; }

        internal void FlipEndianessIfNecessary()
        {
            var span = RawData;
            var itemSizeInBytes = Descriptor.ItemSizeInBytes;
            var length = span.Length / Descriptor.ItemSizeInBytes;
            if (itemSizeInBytes == 2)
            {
                for (var i = 0; i < length; i++)
                {
                    var offset = 2 * i;
                    var temp = span[offset];
                    span[offset] = span[offset + 1];
                    span[offset + 1] = temp;
                }
            }
            else if (itemSizeInBytes == 4)
            {
                for (var i = 0; i < length; i++)
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
            else if (itemSizeInBytes == 8)
            {
                for (var i = 0; i < length; i++)
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

        }

        public long DataCount() => Descriptor.GetFullSize();
        public long DataSizeInBytes() => DataCount() * Descriptor.ItemSizeInBytes;

        protected internal Block(Descriptor desc)
        {
            
            if (desc.IsEmpty)
                throw new ArgumentException(SR.InvalidArgument, nameof(desc));
            Descriptor = desc;
            Keys = new List<IFitsValue>(desc.Nkeys);
        }

        public static Type? ConvertBitPixToType(int bitpix)
        {
            return bitpix switch
            {
                8 => typeof(byte),
                16 => typeof(short),
                32 => typeof(int),
                -32 => typeof(float),
                -64 => typeof(double),
                _ => null
            };
        }

        public static Block Create(Descriptor desc)
        {
            AllowedTypes.ValidateDataType(desc.DataType);
            
            if(desc.DataType == typeof(float))
                return new Block<float>(desc);
            if(desc.DataType == typeof(double))
                return new Block<double>(desc);
            if(desc.DataType == typeof(byte))
                return new Block<byte>(desc);
            if(desc.DataType == typeof(short))
                return new Block<short>(desc);
            if(desc.DataType == typeof(int))
                return new Block<int>(desc);

            throw new ArgumentException(SR.InvalidArgument, nameof(desc));
        }
    }

    public class Block<T> : Block where T : unmanaged
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
}
