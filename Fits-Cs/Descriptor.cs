#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FitsCs
{
    public readonly struct Descriptor
    {
        public ExtensionType Type { get; }

        public bool IsPrimary => Type == ExtensionType.Primary;
        
        public bool IsExtension => !IsPrimary;
        
        public byte ItemSizeInBytes { get; }
        
        public Type DataType { get; }
        
        public ImmutableArray<int> Dimensions { get; }
        
        public int AlignedNumKeys { get; }
        
        public int ParamCount { get; }
        
        public int GroupCount { get; }
        
        public bool IsEmpty =>
            ItemSizeInBytes == 0 && AlignedNumKeys == 0 && DataType == null && ParamCount == 0 && GroupCount == 0;

        public Descriptor(sbyte bitpix, int nKeys, 
            IEnumerable<int> naxis,
            ExtensionType type = ExtensionType.Primary,
            int paramCount = 0, int groupCount = 1)
        {
            if (nKeys < 0)
                throw new ArgumentOutOfRangeException(nameof(nKeys));

            if (paramCount < 0)
                throw new ArgumentOutOfRangeException(nameof(paramCount));
            if (groupCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupCount));

            Type = type;
            var dataType = Extensions.ConvertBitPixToType(bitpix);
            DataType = dataType ?? throw new ArgumentException(nameof(bitpix), SR.InvalidArgument);
            ItemSizeInBytes = (byte)((bitpix < 0 ? -bitpix : bitpix) / 8);
            Dimensions = naxis?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(naxis));
            AlignedNumKeys = nKeys;
            ParamCount = paramCount;
            GroupCount = groupCount;
        }

        internal Descriptor(IReadOnlyList<IFitsValue> header)
        {
            // Organize according to guidelines:
            // 1) SIMPLE/XTENSION
            // 2) BITPIX
            // 3) NAXIS
            // 4) NAXIS1..NAXISN
            // ??
            // 5) PCOUNT & GCOUNT

            if (header.Count < DataBlob.SizeInBytes / FitsKey.EntrySizeInBytes)
                throw new ArgumentException(SR.InvalidArgument, nameof(header));

            var first = header[0];
            Type = ParsingExtensions.FitsExtensionTypeFromString(first.Name switch
            {
                @"SIMPLE" when first is IFitsValue<bool> simpleKey && simpleKey.RawValue => null,
                @"XTENSION" when first is IFitsValue<string> extDesc => extDesc.RawValue,
                _ => throw new InvalidOperationException(SR.InvalidKey)

            });

            var bitPix =
                header[1] switch
                {
                    IFitsValue<int> bpxKey when bpxKey.Name == @"BITPIX" => bpxKey.RawValue,
                    _ => throw new InvalidOperationException(SR.InvalidKey)
                };

            DataType = Extensions.ConvertBitPixToType(bitPix)
                       ?? throw new InvalidOperationException(SR.InvalidKey);
            ItemSizeInBytes = (byte) ((bitPix < 0 ? -bitPix : bitPix) / 8);


            var nAxis = header[2] switch
            {
                IFitsValue<int> naxisKey
                    when
                        naxisKey.Name == @"NAXIS"
                        && naxisKey.RawValue >= 0
                    => naxisKey.RawValue,

                _ => throw new InvalidOperationException(SR.InvalidKey)
            };

            var builder = new int[nAxis];

            for (var i = 0; i < nAxis; i++)
            {
                builder[i] = header[3 + i] switch
                {
                    IFitsValue<int> naxisKey 
                    when
                        naxisKey.Name == $@"NAXIS{i + 1}" 
                        && naxisKey.RawValue >= 0
                    => naxisKey.RawValue,
                    _ => throw new InvalidOperationException(SR.InvalidKey)
                };
            }


            ParamCount = -1;
            GroupCount = -1;

            foreach (var key in header.Skip(3 + nAxis))
            {
                switch (key.Name)
                {
                    case @"PCOUNT" when key is IFitsValue<int> pCountKey:
                        ParamCount = pCountKey.RawValue;
                        break;
                    case @"GCOUNT" when key is IFitsValue<int> gCountKey:
                        GroupCount = gCountKey.RawValue;
                        break;
                }

                if (ParamCount != -1 && GroupCount != -1)
                    break;
            }

            ParamCount = ParamCount == -1 ? 0 : ParamCount;
            GroupCount = GroupCount == -1 ? 1 : GroupCount;

            Dimensions = builder.ToImmutableArray();
            AlignedNumKeys = (header.Count + FitsKey.KeysPerUnit - 1) / FitsKey.KeysPerUnit;
        }

        public long GetFullSize()
        {
            if (Dimensions.IsEmpty)
                return 0;

            var prod = 1L;

            // A signature of oldish random group
            if (Dimensions.Length > 1 && Dimensions[0] == 0)
                for (var i = 1; i < Dimensions.Length; i++)
                    prod *= Dimensions[i];
            else
                foreach (var item in Dimensions)
                    prod *= item;

            return (prod + ParamCount) * GroupCount;
        }
    }
}
