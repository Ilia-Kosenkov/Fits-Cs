using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace FitsCs
{
    public readonly struct Descriptor
    {
        [PublicAPI]
        public bool IsPrimary { get; }
        [PublicAPI]
        public bool IsExtension => !IsPrimary;
        [PublicAPI]
        public byte ItemSizeInBytes { get; }
        [PublicAPI]
        public Type DataType { get; }
        [PublicAPI]
        public ImmutableArray<int> Dimensions { get; }
        [PublicAPI]
        public int Nkeys { get; }
        [PublicAPI] public bool IsEmpty => ItemSizeInBytes == 0 && Nkeys == 0 && DataType == null;

        public Descriptor(bool isPrimary, sbyte bitpix, int nKeys, params int [] naxis)
        {
            IsPrimary = isPrimary;
            var type = Block.ConvertBitPixToType(bitpix);
            DataType = type ?? throw new ArgumentException(nameof(bitpix), SR.InvalidArgument);
            ItemSizeInBytes = (byte)((bitpix < 0 ? -bitpix : bitpix) / 8);
            Dimensions = naxis.ToImmutableArray();
            Nkeys = nKeys;
        }

        public Descriptor(IReadOnlyList<IFitsValue> header)
        {
            if (header.Count < DataBlob.SizeInBytes / FitsKey.EntrySizeInBytes)
                throw new ArgumentException(SR.InvalidArgument, nameof(header));

            var first = header[0];
            switch (first.Name)
            {
                case @"SIMPLE":
                    IsPrimary = true;
                    break;
                case @"XTENSION":
                    IsPrimary = false;
                    break;
                default:
                    throw new ArgumentException(SR.InvalidArgument, nameof(header));
            }

            var bitPix = 0;
            var nAxis = -1;
            int[] builder = null;
            var count = 0;
            foreach (var key in header)
            {
                if (key.Name == @"BITPIX" && key is IFitsValue<int> bitPixKey)
                {
                    if (bitPix == 0)
                        bitPix = bitPixKey.RawValue;
                    else
                        throw new ArgumentException(SR.InvalidArgument, nameof(header));
                }
                else if (key.Name == @"NAXIS" && key is IFitsValue<int> nAxisKey)
                {
                    if (nAxis == -1 && nAxisKey.RawValue >= 0)
                    {
                        nAxis = nAxisKey.RawValue;
                        builder = new int[nAxis];
                    }
                    else
                        throw new ArgumentException(SR.InvalidArgument, nameof(header));
                }
                else if (key.Name.StartsWith(@"NAXIS")
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

                // If all data are retrieved
                if(bitPix != 0 && count == nAxis)
                    break;

            }

            if(nAxis < 0)
                throw new ArgumentException(SR.InvalidArgument, nameof(header));


            DataType = Block.ConvertBitPixToType(bitPix) ?? throw new ArgumentException(SR.InvalidArgument, nameof(header));
            ItemSizeInBytes = (byte) ((bitPix < 0 ? -bitPix : bitPix) / 8);
            Dimensions = builder?.ToImmutableArray() ?? ImmutableArray<int>.Empty;
            Nkeys = (int)(FitsKey.KeysPerUnit * Math.Ceiling(1.0 * header.Count / FitsKey.KeysPerUnit));
        }

        public long GetFullSize() => Dimensions.Aggregate<int, long>(1, (current, dim) => current * dim);
    }
}
