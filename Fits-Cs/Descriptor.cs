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
        
        public int ParamCount { get; }
        
        public int GroupCount { get; }
        
        public bool IsEmpty =>
            ItemSizeInBytes == 0 && ParamCount == 0 && GroupCount == 0;

        public Descriptor(sbyte bitpix, 
            IEnumerable<int> naxis,
            ExtensionType type = ExtensionType.Primary,
            int paramCount = 0, int groupCount = 1)
        {
            if (paramCount < 0)
                throw new ArgumentOutOfRangeException(nameof(paramCount));
            if (groupCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupCount));
            if (naxis is null)
            {
                throw new ArgumentNullException(nameof(naxis));
            }
            
            
            Type = type;
            var dataType = Extensions.ConvertBitPixToType(bitpix);
            DataType = dataType ?? throw new ArgumentException(nameof(bitpix), SR.InvalidArgument);
            ItemSizeInBytes = (byte)((bitpix < 0 ? -bitpix : bitpix) / 8);
            Dimensions = naxis.ToImmutableArray();
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
            Type = ParsingExtensions.FitsExtensionTypeFromString(
                first switch
                {
                    IFitsValue<bool> {Name: @"SIMPLE", RawValue: true} => null,
                    IFitsValue<string> {Name: @"XTENSION", RawValue: var extDesc} => extDesc,
                    _ => throw new InvalidOperationException(SR.InvalidKey)
                }
            );
                
          
            var bitPix =
                header[1] switch
                {
                    IFitsValue<int> {Name: @"BITPIX", RawValue: var bpxVal} => bpxVal,
                    _ => throw new InvalidOperationException(SR.InvalidKey)
                };

            DataType = Extensions.ConvertBitPixToType(bitPix)
                       ?? throw new InvalidOperationException(SR.InvalidKey);
            ItemSizeInBytes = (byte) ((bitPix < 0 ? -bitPix : bitPix) / 8);


            var nAxis = header[2] switch
            {
                IFitsValue<int> {Name: @"NAXIS", RawValue: var naxisVal and >= 0} => naxisVal,
                _ => throw new InvalidOperationException(SR.InvalidKey)
            };

            var builder = new int[nAxis];

            for (var i = 0; i < nAxis; i++)
            {
                builder[i] = header[3 + i] switch
                {
                    IFitsValue<int> {Name: var naxisName, RawValue: var naxisVal and >= 0}
                        when naxisName == $@"NAXIS{i + 1}"
                        => naxisVal,
                    _ => throw new InvalidOperationException(SR.InvalidKey)
                };
            }


            ParamCount = -1;
            GroupCount = -1;

            foreach (var key in header.Skip(3 + nAxis))
            {
                switch (key)
                {
                    case IFitsValue<int> {Name: @"PCOUNT", RawValue: var pCount}:
                        ParamCount = pCount;
                        break;
                    case IFitsValue<int>{Name:@"GCOUNT", RawValue: var gCount}:
                        GroupCount = gCount;
                        break;
                }
                
                if (ParamCount != -1 && GroupCount != -1)
                {
                    break;
                }
            }

            ParamCount = ParamCount == -1 ? 0 : ParamCount;
            GroupCount = GroupCount == -1 ? 1 : GroupCount;

            Dimensions = builder.ToImmutableArray();
        }

        public long GetFullSize()
        {
            if (Dimensions.IsEmpty)
                return 0;

            var prod = 1L;

            // A signature of oldish random group
            if (Dimensions.Length > 1 && Dimensions[0] == 0)
            {
                for (var i = 1; i < Dimensions.Length; i++)
                {
                    prod *= Dimensions[i];
                }
            }
            else
            {
                foreach (var item in Dimensions)
                {
                    prod *= item;
                }
            }

            return (prod + ParamCount) * GroupCount;
        }

        public ImmutableList<IFitsValue> GenerateFitsHeader()
        {
            var builder = ImmutableList.CreateBuilder<IFitsValue>();
            builder.Add(Type.ToFitsKey());
            builder.Add(FitsKey.Create(
                @"BITPIX",
                (int)(Extensions.ConvertTypeToBitPix(DataType) ?? throw new InvalidOperationException(SR.ShouldNotHappen)),
                KeyComments.Bitpix));
            builder.Add(FitsKey.Create(@"NAXIS", Dimensions.Length, KeyComments.Naxis));

            for(var i = 0; i < Dimensions.Length; i++)
                builder.Add(FitsKey.Create(@$"NAXIS{i + 1}", Dimensions[i]));

            if (ParamCount != 0 || GroupCount != 1)
            {
                builder.Add(FitsKey.Create(@"PCOUNT", ParamCount, KeyComments.Pcount));
                builder.Add(FitsKey.Create(@"GCOUNT", GroupCount, KeyComments.Gcount));
            }

            return builder.ToImmutable();
        }
    }
}
