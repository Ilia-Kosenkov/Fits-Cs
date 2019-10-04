using System;
using System.Numerics;
using Maybe;

namespace FitsCs.Keys
{
    public abstract class FreeFitsKey : FitsKey
    {
        private protected static readonly string EmptyString = string.Intern(new string (' ', 20));

        public override KeyType Type => KeyType.Free;
        private protected FreeFitsKey(string name, string comment) : base(name, comment)
        {
        }

        //private protected bool FormatFree(Span<char> span, string value)
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
                    return new FreeDoubleKey(name, dVal, comment) as IFitsValue<T>;
                case Maybe<float> fVal:
                    return new FreeFloatKey(name, fVal, comment) as IFitsValue<T>;
                case Maybe<int> iVal:
                    return new FreeIntKey(name, iVal, comment) as IFitsValue<T>;
                case Maybe<bool> bVal:
                    return new FreeBoolKey(name, bVal, comment) as IFitsValue<T>;
                case Maybe<Complex> cVal:
                    return new FreeComplexKey(name, cVal, comment) as IFitsValue<T>;
                case Maybe<string> sval:
                    return new FreeStringKey(name, sval, comment) as IFitsValue<T>;
            }
            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }

        public static IFitsValue Create(string name, Maybe.Maybe value, string comment = null)
        {

            switch (value)
            {
                case Maybe<double> dVal:
                    return new FreeDoubleKey(name, dVal, comment) as IFitsValue;
                case Maybe<float> fVal:
                    return new FreeFloatKey(name, fVal, comment) as IFitsValue;
                case Maybe<int> iVal:
                    return new FreeIntKey(name, iVal, comment) as IFitsValue;
                case Maybe<bool> bVal:
                    return new FreeBoolKey(name, bVal, comment) as IFitsValue;
                case Maybe<Complex> cVal:
                    return new FreeComplexKey(name, cVal, comment) as IFitsValue;
                case Maybe<string> sval:
                    return new FreeStringKey(name, sval, comment) as IFitsValue;
            }
            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }
    }
}