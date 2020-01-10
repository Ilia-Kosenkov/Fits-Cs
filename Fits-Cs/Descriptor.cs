using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

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
        
        public int Nkeys { get; }
        
        public int ParamCount { get; }
        
        public int GroupCount { get; }
        
        public bool IsEmpty =>
            ItemSizeInBytes == 0 && Nkeys == 0 && DataType == null && ParamCount == 0 && GroupCount == 0;

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
            var dataType = Block.ConvertBitPixToType(bitpix);
            DataType = dataType ?? throw new ArgumentException(nameof(bitpix), SR.InvalidArgument);
            ItemSizeInBytes = (byte)((bitpix < 0 ? -bitpix : bitpix) / 8);
            Dimensions = naxis?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(naxis));
            Nkeys = nKeys;
            ParamCount = paramCount;
            GroupCount = groupCount;
        }

        public Descriptor(IReadOnlyList<IFitsValue> header)
        {
            if (header.Count < DataBlob.SizeInBytes / FitsKey.EntrySizeInBytes)
                throw new ArgumentException(SR.InvalidArgument, nameof(header));

            var first = header[0];
            Type = ParsingExtensions.FitsExtensionTypeFromString(first.Name switch
            {
                @"SIMPLE" when first is IFitsValue<bool> simpleKey && simpleKey.RawValue => null,
                @"XTENSION" when first is IFitsValue<string> extDesc => extDesc.RawValue,
                _ => throw new ArgumentException(SR.InvalidArgument, nameof(header))
            });

            var bitPix = 0;
            var nAxis = -1;
            int[] builder = null;
            var count = 0;
            var nGroups = -1;
            var nParams = -1;

            // TODO : optimize iterations
            foreach (var key in header)
            {
                switch (key.Name)
                {
                    case @"BITPIX" when key is IFitsValue<int> bitPixKey:
                    {
                        if (bitPix == 0)
                            bitPix = bitPixKey.RawValue;
                        else
                            throw new ArgumentException(SR.InvalidArgument, nameof(header));
                        break;
                    }
                    case @"NAXIS" when key is IFitsValue<int> nAxisKey:
                    {
                        if (nAxis == -1 && nAxisKey.RawValue >= 0)
                        {
                            nAxis = nAxisKey.RawValue;
                            builder = new int[nAxis];
                        }
                        else
                            throw new ArgumentException(SR.InvalidArgument, nameof(header));

                        break;
                    }
                    case @"PCOUNT" when key is IFitsValue<int> pCountKey:
                    {
                        if (nParams == -1)
                            nParams = pCountKey.RawValue;
                        else
                            throw new ArgumentException(SR.InvalidArgument, nameof(header));
                        break;
                    }
                    case @"GCOUNT" when key is IFitsValue<int> gCountKey:
                    {
                        if (nGroups == -1)
                            nGroups = gCountKey.RawValue;
                        else
                            throw new ArgumentException(SR.InvalidArgument, nameof(header));
                        break;
                    }
                    default:
                    {
                        if (key.Name?.StartsWith(@"NAXIS") == true
                            && count < nAxis 
                            && key is IFitsValue<int> subNaxisKey)
                        {
                            if (int.TryParse(key.Name.Substring(5), NumberStyles.Any, NumberFormatInfo.InvariantInfo,
                                    out var subNaxis)&& subNaxis > 0 && subNaxis <= nAxis)
                            {
                                if(builder is null || subNaxisKey.RawValue < 0)
                                    throw new ArgumentException(SR.InvalidArgument, nameof(header));
                                builder[subNaxis - 1] = subNaxisKey.RawValue;
                                count++;
                            }
                        }

                        break;
                    }
                }
            }

            if(nAxis < 0)
                throw new ArgumentException(SR.InvalidArgument, nameof(header));


            DataType = Block.ConvertBitPixToType(bitPix) ?? throw new ArgumentException(SR.InvalidArgument, nameof(header));
            ItemSizeInBytes = (byte) ((bitPix < 0 ? -bitPix : bitPix) / 8);
            Dimensions = builder?.ToImmutableArray() ?? ImmutableArray<int>.Empty;
            Nkeys = (int)(FitsKey.KeysPerUnit * Math.Ceiling(1.0 * header.Count / FitsKey.KeysPerUnit));
            ParamCount = nParams == -1 ? 0 : nParams;
            GroupCount = nGroups == -1 ? 1 : nGroups;
        }

        // TODO : Accelerate computation instead of using LINQ
        public long GetFullSize() //=>
            //(Dimensions.Aggregate<int, long>(1, (current, dim) => current * dim) + ParamCount) * GroupCount;
        {
            if (Dimensions.IsEmpty)
                return 0;

            var prod = 1L;
            foreach (var item in Dimensions)
                prod *= item;

            return (prod + ParamCount) * GroupCount;
        }
    }
}
