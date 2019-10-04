using System;
using System.Numerics;
using Maybe;

namespace FitsCs.Keys
{
    public abstract class FixedFitsKey : FitsKey
    {
        protected const int FixedFieldSize = 20;
        public override KeyType Type => KeyType.Fixed;
        private protected FixedFitsKey(string name, string comment) : base(name, comment)
        {
        }

        //private protected bool FormatFixed(Span<char> span, string value)
        //{
        //    if (span.Length < EntrySizeInBytes)
        //        return false;

        //    var isCommentNull = string.IsNullOrWhiteSpace(Comment);
        //    var len = NameSize +
        //              value.Length;

        //    span.Slice(0, EntrySizeInBytes).Fill(' ');
        //    Name.AsSpan().CopyTo(span);
        //    value.AsSpan().CopyTo(span.Slice(NameSize));


        //    if (!isCommentNull)
        //    {
        //        Comment.AsSpan().CopyTo(span.Slice(len + 2));
        //        span[len + 1] = '/';
        //    }

        //    return true;
        //}

        public static IFitsValue<T> Create<T>(string name, Maybe<T> value, string comment = null)
        {
            //ValidateType<T>();

            switch (value)
            {
                case Maybe<double> dVal:
                    return new FixedDoubleKey(name, dVal, comment) as IFitsValue<T>;
                case Maybe<float> fVal:
                    return new FixedFloatKey(name, fVal, comment) as IFitsValue<T>;
                case Maybe<int> iVal:
                    return new FixedIntKey(name, iVal, comment) as IFitsValue<T>;
                case Maybe<bool> bVal:
                    return new FixedBoolKey(name, bVal, comment) as IFitsValue<T>;
                case Maybe<Complex> cVal:
                    return new FixedComplexKey(name, cVal, comment) as IFitsValue<T>;
                case Maybe<string> sVal:
                    return new FixedStringKey(name, sVal, comment) as IFitsValue<T>;
            }
            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }

        public static IFitsValue Create(string name, Maybe.Maybe value, string comment = null)
        {
            switch (value)
            {
                case Maybe<double> dVal:
                    return new FixedDoubleKey(name, dVal, comment);
                case Maybe<float> fVal:
                    return new FixedFloatKey(name, fVal, comment);
                case Maybe<int> iVal:
                    return new FixedIntKey(name, iVal, comment);
                case Maybe<bool> bVal:
                    return new FixedBoolKey(name, bVal, comment);
                case Maybe<Complex> cVal:
                    return new FixedComplexKey(name, cVal, comment);
                case Maybe<string> sVal:
                    return new FixedStringKey(name, sVal, comment);
            }
            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }
    }
}