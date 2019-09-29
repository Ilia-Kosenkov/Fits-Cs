using System;
using System.Numerics;
using Maybe;

namespace FitsCs
{
    public abstract class FixedFitsKey : FitsKey
    {
        private protected static readonly string EmptyString = string.Intern(new string (' ', 20));

        public override KeyType Type => KeyType.Fixed;
        private protected FixedFitsKey(string name, string comment) : base(name, comment)
        {
        }

        private protected bool FormatFixed(Span<char> span, string value)
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            var len = NameSize +
                      value.Length;

            if (span.Length < EntrySizeInBytes)
                return false;

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            Name.AsSpan().CopyTo(span);
            value.AsSpan().CopyTo(span.Slice(NameSize));


            if (!isCommentNull)
            {
                Comment.AsSpan().CopyTo(span.Slice(len + 2));
                span[len + 1] = '/';
            }

            return true;
        }

        public static IFitsValue<T> Create<T>(string name, Maybe<T> value, string comment = null)
        {
            ValidateType<T>();


            switch (value)
            {
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
            throw new NotSupportedException();
        }
    }
}