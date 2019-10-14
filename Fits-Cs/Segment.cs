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

    public sealed class ExtensionBLock<T> : Block<T>
    {
        public override bool IsPrimary => false;

        public ExtensionBLock(int nKeyUnits, int nDataUnits) : base(nKeyUnits, nDataUnits)
        {
        }
    }
}
