using System;
using System.Numerics;

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

        public static IFitsValue<T> Create<T>(string name, T value, string comment = null)
        {
            //ValidateType<T>();

            switch (value)
            {
                case double dVal:
                    return new FreeDoubleKey(name, dVal, comment) as IFitsValue<T>;
                case float fVal:
                    return new FreeFloatKey(name, fVal, comment) as IFitsValue<T>;
                case int iVal:
                    return new FreeIntKey(name, iVal, comment) as IFitsValue<T>;
                case bool bVal:
                    return new FreeBoolKey(name, bVal, comment) as IFitsValue<T>;
                case Complex cVal:
                    return new FreeComplexKey(name, cVal, comment) as IFitsValue<T>;
                case string nullStr when nullStr is null:
                    throw new ArgumentNullException(nameof(value), SR.NullArgument);
                case string sVal:
                    return new FreeStringKey(name, sVal, comment) as IFitsValue<T>;
            }
            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }

        public static IFitsValue Create(string name, object value, string comment = null)
        {

            switch (value)
            {
                case double dVal:
                    return new FreeDoubleKey(name, dVal, comment);
                case float fVal:
                    return new FreeFloatKey(name, fVal, comment);
                case int iVal:
                    return new FreeIntKey(name, iVal, comment);
                case bool bVal:
                    return new FreeBoolKey(name, bVal, comment);
                case Complex cVal:
                    return new FreeComplexKey(name, cVal, comment);
                case string nullStr when nullStr is null:
                    throw new ArgumentNullException(nameof(value), SR.NullArgument);
                case string sVal:
                    return new FreeStringKey(name, sVal, comment);
            }

            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }
    }
}