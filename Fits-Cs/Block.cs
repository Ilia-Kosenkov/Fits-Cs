using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FitsCs
{
    public abstract class Block
    {
        private protected readonly List<IFitsValue> _keys;
        public IList<IFitsValue> Keys => _keys;

        public abstract bool IsPrimary { get; }

        protected internal Block(int nKeyUnits)
        {
            if (nKeyUnits <= 0)
                throw new ArgumentException(SR.InvalidArgument, nameof(nKeyUnits));
            _keys = new List<IFitsValue>(nKeyUnits * FitsKey.KeysPerUnit);
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

        public static Block Create(Descriptor desc)
        {
            AllowedTypes.ValidateDataType(desc.DataType);

            var nDataUnits = (int)(desc.GetFullSize() * desc.ItemSizeInBytes / DataBlob.SizeInBytes);
            var nKeyUnits = desc.Nkeys / FitsKey.KeysPerUnit;
            if (desc.IsPrimary)
            {
                if(desc.DataType == typeof(float))
                    return new PrimaryBlock<float>(nKeyUnits, nDataUnits);
                if(desc.DataType == typeof(double))
                    return new PrimaryBlock<double>(nKeyUnits, nDataUnits);
                if(desc.DataType == typeof(byte))
                    return new PrimaryBlock<byte>(nKeyUnits, nDataUnits);
                if(desc.DataType == typeof(short))
                    return new PrimaryBlock<byte>(nKeyUnits, nDataUnits);
                if(desc.DataType == typeof(int))
                    return new PrimaryBlock<byte>(nKeyUnits, nDataUnits);

                throw new ArgumentException(SR.InvalidArgument, nameof(desc));
            }

            if (desc.DataType == typeof(float))
                return new ExtensionBlock<float>(nKeyUnits, nDataUnits);
            if (desc.DataType == typeof(double))
                return new ExtensionBlock<double>(nKeyUnits, nDataUnits);
            if (desc.DataType == typeof(byte))
                return new ExtensionBlock<byte>(nKeyUnits, nDataUnits);
            if (desc.DataType == typeof(short))
                return new ExtensionBlock<byte>(nKeyUnits, nDataUnits);
            if (desc.DataType == typeof(int))
                return new ExtensionBlock<byte>(nKeyUnits, nDataUnits);

            throw new ArgumentException(SR.InvalidArgument, nameof(desc));

        }
    }

    public abstract class Block<T> : Block
    {
        private protected T[] Data;
        public Span<T> RawData => Data ?? Span<T>.Empty;

        protected internal Block(int nKeyUnits, int nDataUnits) : base(nKeyUnits)
        {
            AllowedTypes.ValidateDataType<T>();

            if (nDataUnits < 0)
                throw new ArgumentException(SR.InvalidArgument, nameof(nDataUnits));

            Data = new T[nDataUnits * DataBlob.SizeInBytes / Unsafe.SizeOf<T>()];
        }
    }

    public sealed class PrimaryBlock<T> : Block<T>
    {
        public override bool IsPrimary => true;

        public PrimaryBlock(int nKeyUnits, int nDataUnits) : base(nKeyUnits, nDataUnits)
        {
           
        }

    }

    public sealed class ExtensionBlock<T> : Block<T>
    {
        public override bool IsPrimary => false;

        public ExtensionBlock(int nKeyUnits, int nDataUnits) : base(nKeyUnits, nDataUnits)
        {
        }
    }
}
