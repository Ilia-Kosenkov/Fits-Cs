﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using FitsCs.Keys;

namespace FitsCs
{
    public static class Extensions
    {

        private const int MinFixedStringSize = FixedFitsKey.FixedFieldSize - FitsKey.ValueStart;
        internal static int StringSizeWithQuoteReplacement(
            this ReadOnlySpan<char> s,
            int minLength = MinFixedStringSize)
        {
            var sum = 2;
            foreach (var item in s)
            {
                // Regular character 1-to-1
                sum += 1;
                // Single quote 2-to-1
                if (item == '\'')
                {
                    sum += 1;
                }
                // >>> Double quotes are treated as a single character, according to the standard
                //// Double quote replaced by 4 single quotes
                //if (item == '"')
                //    sum += 3;
            }

            return sum < minLength ? minLength : sum;
        }

        internal static bool TryGetCompatibleString(
            this ReadOnlySpan<char> source, 
            Span<char> target,
            int minLength = MinFixedStringSize)
        {

            if (target.Length < source.Length + 2)
            {
                return false;
            }

            target[0] = '\'';
            var srcInd = 0;
            var targetInd = 1;
            
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] == '\'')
                {
                    if (!source[srcInd..(i + 1)].TryCopyTo(target.Slice(targetInd)))
                    {
                        return false;
                    }
                    targetInd += i - srcInd + 1;
                    target[targetInd++] = '\'';
                    srcInd = i + 1;
                }
                // >>> No replacement of double quotes
                //else if (source[i] == '"')
                //{
                //    if (!source.Slice(srcInd..i).TryCopyTo(target.Slice(targetInd)))
                //        return false;
                //    targetInd += i - srcInd;
                //    target.Slice(targetInd, 4).Fill('\'');
                //    targetInd += 4;
                //    srcInd = i + 1;
                //}

            }

            if (srcInd < source.Length)
            {
                if (!source.Slice(srcInd).TryCopyTo(target.Slice(targetInd)))
                {
                    return false;
                }
                targetInd += source.Length - srcInd;
            }

            if (targetInd >= target.Length)
            {
                return false;
            }

            if (targetInd < minLength - 1)
            {
                target[targetInd..(minLength - 1)].Fill(' ');
                targetInd = minLength - 1;
            }

            target[targetInd] = '\'';

            return true;
        }

        internal static (int NumSrcSymb, int NumConvSymb) MaxCompatibleStringSize(
            this ReadOnlySpan<char> source,
            int length)
        {
            if (length <= 1)
            {
                return (source.Length, 0);
            }

            var srcId = 0;
            var numConvSymb = 2;
            for(; numConvSymb < length & srcId < source.Length; srcId++)
            {
                // Regular character 1-to-1
                numConvSymb += 1;
                // Single quote 2-to-1
                if (source[srcId] == '\'')
                    numConvSymb += 1;
                // >>> No replacement of double quotes
                //// Double quote replaced by 4 single quotes
                //if (source[srcId] == '"')
                //    numConvSymb += 3;
            }

            return (srcId, numConvSymb);
        }

        internal static int SignificantDigitsCount(this int value)
        {
            if (value == 0)
            {
                return 1;
            }

            if (value < 0)
            {
                value = -value;
            }

            var n = 0;

            while (value != 0)
            {
                n++;
                value /= 10;
            }

            return n;
        }

        internal static int SignificantDigitsCount(this long value)
        {
            if (value == 0)
            {
                return 1;
            }

            if (value < 0)
            {
                value = -value;
            }

            var n = 0;

            while (value != 0)
            {
                n++;
                value /= 10;
            }

            return n;
        }

        internal static bool TryFormatDouble(this double value, uint decPos, uint maxSize, Span<char> target)
        {
            Span<char> format = stackalloc char[12];
            format[0] = 'G';
            if (!decPos.TryFormat(format[1..], out _))
                return false;
            
            var reTry = !value.TryFormat(target, out var nChars, format);
            if (reTry && !(maxSize - 7).TryFormat(format[1..], out _) ||
                !value.TryFormat(target, out nChars, format))
            {
                reTry = true;
            }

            if (reTry && !(maxSize - 6).TryFormat(format[1..], out _) ||
                !value.TryFormat(target, out nChars, format))
            {
                return false;
            }

            if (nChars > maxSize)
            {
                return false;
            }

            Span<char> data = target.Slice(0, nChars);

            if (!data.Contains('.'))
            {
                data.Fill('\0');
                format.Fill('\0');
                format[0] = 'F';
                format[1] = '1';

                if (!value.TryFormat(target, out nChars, format))
                    return false;
            }

            if (nChars > maxSize)
                return false;

            target.Slice(0, nChars).CopyTo(target.Slice(target.Length - nChars));
            target.Slice(0, target.Length - nChars).Fill(' ');

            return true;
        }

        internal static bool TryFormatFloat(this float value, uint decPos, uint maxSize, Span<char> target)
        {
            Span<char> format = stackalloc char[12];
            format[0] = 'G';
            if (!decPos.TryFormat(format[1..], out _))
            {
                return false;
            }

            if (!value.TryFormat(target, out var nChars, format))
            {
                return false;
            }

            if (nChars > maxSize)
            {
                return false;
            }

            Span<char> data = target.Slice(0, nChars);

            if (!data.Contains('.'))
            {
                data.Fill('\0');
                format.Fill('\0');
                format[0] = 'F';
                format[1] = '1';

                if (!value.TryFormat(target, out nChars, format))
                {
                    return false;
                }
            }
            
            target.Slice(0, nChars).CopyTo(target.Slice(target.Length - nChars));
            target.Slice(0, target.Length - nChars).Fill(' ');

            return true;
        }

        internal static bool IsStringHduCompatible(this ReadOnlySpan<char> @string, Encoding? enc = null)
        {
            enc ??= Encoding.ASCII;

            if (@string.IsEmpty)
            {
                return true;
            }

            var byteSize = enc.GetByteCount(@string);

            Span<byte> buff = @string.Length < 512 ? stackalloc byte[byteSize] : new byte[byteSize];

            var n = enc.GetBytes(@string, buff);
            
            return n > 0 && buff.Slice(0, n).All(x => x >= 0x20 && x <= 0x7E);
        }

        public static bool Contains<T>(this Span<T> span, T value)
            where T : unmanaged, IEquatable<T>
        {
            ref var offset = ref span[0];
            if (offset.Equals(value))
                return true;

            for(var i = 1; i < span.Length; i++)
            {
                offset = ref Unsafe.Add(ref offset, 1);
                if (offset.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool All<T>(this Span<T> span, Func<T, bool> predicate)
            where T : unmanaged
        {
            ref var offset = ref span[0];
            if (!predicate(offset))
            {
                return false;
            }

            for (var i = 1; i < span.Length; i++)
            {
                offset = ref Unsafe.Add(ref offset, 1);
                if (!predicate(offset))
                {
                    return false;
                }
            }
            return true;
        }

        public static IFitsValue? With<T>(
            this IFitsValue<T> @this, 
            Action<KeyUpdater> updateAction)
        {
            if (@this is null)
            {
                throw new ArgumentNullException(nameof(@this), SR.NullArgument);
            }

            if (updateAction is null)
            {
                throw new ArgumentNullException(nameof(updateAction), SR.NullArgument);
            }

            var updater = new KeyUpdater
            {
                Name = @this.Name,
                Comment = @this.Comment,
                Value = @this.RawValue,
                Type = @this.Type
            };

            updateAction(updater);

            // If it creation fails, return null
            try
            {
                return FitsKey.Create(updater.Name, updater.Value, updater.Comment, updater.Type);
            }
            catch
            {
                return null;
            }
        }


        public static IFitsValue? GetFirstByName(this IReadOnlyList<IFitsValue> keys, string name)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys), SR.NullArgument);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), SR.NullArgument);
            }

            return keys.FirstOrDefault(item => item?.Name == name);
        }

        public static bool IsEnd(this IReadOnlyList<IFitsValue> keys)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys), SR.NullArgument);
            }

            for(var i = keys.Count - 1; i >= 0; i--)
            {
                if (keys[i] is not {Name: @"END"}) continue;
                
                return true;
            }

            return false;
        }

        public static void FlipEndianess(this Span<byte> span, int itemSizeInBytes)
        {
            if (span.IsEmpty)
            {
                return;
            }
            
            var length = span.Length / itemSizeInBytes;

            switch (itemSizeInBytes)
            {
                case 2:
                {
                    for (var i = 0; i < length; i++)
                    {
                        var offset = 2 * i;
                        var temp = span[offset];
                        span[offset] = span[offset + 1];
                        span[offset + 1] = temp;
                    }

                    break;
                }
                case 4:
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

                    break;
                }
                case 8:
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

                    break;
                }
            }
        }

        public static IFitsValue ToFitsKey(this ExtensionType type)
            => type switch
            {
                ExtensionType.Image => FitsKey.Create(@"XTENSION", @"IMAGE", KeyComments.Xtension_Image),
                ExtensionType.BinTable => FitsKey.Create(@"XTENSION", @"BINTABLE", KeyComments.Xtension_Bintable),
                ExtensionType.Table => FitsKey.Create(@"XTENSION", @"TABLE", KeyComments.Xtension_Table),
                ExtensionType.Primary => FitsKey.Create(@"SIMPLE", true, KeyComments.Simple_True),
                _ => throw new NotSupportedException(SR.HduTypeNotSupported)
            };

        public static Type? ConvertBitPixToType(int bitpix)
        {
            return bitpix switch
            {
                8 => typeof(byte),
                16 => typeof(short),
                32 => typeof(int),
                64 => typeof(long),
                -32 => typeof(float),
                -64 => typeof(double),
                _ => null
            };
        }

        public static sbyte? ConvertTypeToBitPix(Type type)
            => AllowedTypes.CanBeDataType(type)
                ? (sbyte?) (type switch
                {
                    _ when type == typeof(float) => sizeof(float) * -8,
                    _ when type == typeof(double) => sizeof(double) * -8,
                    _ => Marshal.SizeOf(type) * 8
                })
                : null;

        public static sbyte? ConvertTypeToBitPix<T>()
            where T : unmanaged
            => AllowedTypes.CanBeDataType<T>()
                ? (sbyte?) (default(T) switch
                {
                    float _ => sizeof(float) * -8,
                    double _ => sizeof(double) * -8,
                    _ => Unsafe.SizeOf<T>() * 8
                })
                : null;

        public static long PadData(long size) 
            => DataBlob.SizeInBytes * ((size + DataBlob.SizeInBytes - 1) / DataBlob.SizeInBytes);
    }
}
