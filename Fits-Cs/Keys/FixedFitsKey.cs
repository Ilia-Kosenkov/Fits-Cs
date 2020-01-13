#nullable enable
using System;
using System.Numerics;

namespace FitsCs.Keys
{
    public abstract class FixedFitsKey : FitsKey
    {
        public const int FixedFieldSize = 20;
        public override KeyType Type => KeyType.Fixed;
        public override bool IsEmpty => false;

        private protected FixedFitsKey(string name, string? comment, int size) : base(name, comment, size)
        {
        }

        public static IFitsValue<T> Create<T>(string name, T value, string? comment = null) =>
            value switch
            {
                double dVal => (new FixedDoubleKey(name, dVal, comment) as IFitsValue<T>),
                float fVal => (new FixedFloatKey(name, fVal, comment) as IFitsValue<T>),
                int iVal => (new FixedIntKey(name, iVal, comment) as IFitsValue<T>),
                long lVal => (new FixedLongKey(name, lVal, comment) as IFitsValue<T>),
                bool bVal => (new FixedBoolKey(name, bVal, comment) as IFitsValue<T>),
                Complex cVal => (new FixedComplexKey(name, cVal, comment) as IFitsValue<T>),
                string sVal => (new FixedStringKey(name, sVal, comment) as IFitsValue<T>),
                _ => throw new NotSupportedException(SR.KeyTypeNotSupported)
            } ?? throw new NullReferenceException(SR.UnexpectedNullRef);

        public static IFitsValue Create(string name, object? value, string? comment = null) =>
            value is null
                ? throw new ArgumentNullException(nameof(value), SR.NullArgument)
                : value switch
                {
                    double dVal => (IFitsValue) new FixedDoubleKey(name, dVal, comment),
                    float fVal => new FixedFloatKey(name, fVal, comment),
                    int iVal => new FixedIntKey(name, iVal, comment),
                    long lVal => new FixedLongKey(name, lVal, comment),
                    bool bVal => new FixedBoolKey(name, bVal, comment),
                    Complex cVal => new FixedComplexKey(name, cVal, comment),
                    string sVal => new FixedStringKey(name, sVal, comment),
                    _ => throw new NotSupportedException(SR.KeyTypeNotSupported)
                };
    }
}