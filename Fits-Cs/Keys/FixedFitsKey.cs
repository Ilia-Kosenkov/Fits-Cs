using System;
using System.Numerics;

namespace FitsCs.Keys
{
    public abstract class FixedFitsKey : FitsKey
    {
        public const int FixedFieldSize = 20;
        public override KeyType Type => KeyType.Fixed;
        public override bool IsEmpty => false;

        private protected FixedFitsKey(string name, string comment) : base(name, comment)
        {
        }
        public static IFitsValue<T> Create<T>(string name, T value, string comment = null)
        {
            //ValidateType<T>();

            switch (value)
            {
                case double dVal:
                    return new FixedDoubleKey(name, dVal, comment) as IFitsValue<T>;
                case float fVal:
                    return new FixedFloatKey(name, fVal, comment) as IFitsValue<T>;
                case int iVal:
                    return new FixedIntKey(name, iVal, comment) as IFitsValue<T>;
                case bool bVal:
                    return new FixedBoolKey(name, bVal, comment) as IFitsValue<T>;
                case Complex cVal:
                    return new FixedComplexKey(name, cVal, comment) as IFitsValue<T>;
                case string nullStr when nullStr is null:
                    throw new ArgumentNullException(nameof(value), SR.NullArgument);
                case string sVal:
                    return new FixedStringKey(name, sVal, comment) as IFitsValue<T>;
            }
            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }

        public static IFitsValue Create(string name, object value, string comment = null)
        {
            switch (value)
            {
                case double dVal:
                    return new FixedDoubleKey(name, dVal, comment);
                case float fVal:
                    return new FixedFloatKey(name, fVal, comment);
                case int iVal:
                    return new FixedIntKey(name, iVal, comment);
                case bool bVal:
                    return new FixedBoolKey(name, bVal, comment);
                case Complex cVal:
                    return new FixedComplexKey(name, cVal, comment);
                case string nullStr when nullStr is null:
                    throw new ArgumentNullException(nameof(value), SR.NullArgument);
                case string sVal:
                    return new FixedStringKey(name, sVal, comment);
            }

            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }
    }
}