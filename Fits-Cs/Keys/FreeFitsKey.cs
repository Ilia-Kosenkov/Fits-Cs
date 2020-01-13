#nullable enable
using System;
using System.Numerics;

namespace FitsCs.Keys
{
    public abstract class FreeFitsKey : FitsKey
    {
        public override KeyType Type => KeyType.Free;
        private protected FreeFitsKey(string name, string? comment, int size) 
            : base(name, comment, size)
        {
        }

        public static IFitsValue<T> Create<T>(string name, T value, string? comment = null) =>
            value switch
            {
                double dVal => (new FreeDoubleKey(name, dVal, comment) as IFitsValue<T>),
                float fVal => (new FreeFloatKey(name, fVal, comment) as IFitsValue<T>),
                int iVal => (new FreeIntKey(name, iVal, comment) as IFitsValue<T>),
                long lVal => (new FreeLongKey(name, lVal, comment) as IFitsValue<T>),
                bool bVal => (new FreeBoolKey(name, bVal, comment) as IFitsValue<T>),
                Complex cVal => (new FreeComplexKey(name, cVal, comment) as IFitsValue<T>),
                string sVal => (new FreeStringKey(name, sVal, comment) as IFitsValue<T>),
                _ => throw new NotSupportedException(SR.KeyTypeNotSupported)
            } ?? throw new NullReferenceException(SR.UnexpectedNullRef);

        public static IFitsValue Create(string name, object? value, string? comment = null) =>
            value is null
                ? throw new ArgumentNullException(nameof(value), SR.NullArgument)
                : value switch
                {
                    double dVal => (IFitsValue) new FreeDoubleKey(name, dVal, comment),
                    float fVal => new FreeFloatKey(name, fVal, comment),
                    int iVal => new FreeIntKey(name, iVal, comment),
                    long lVal => new FreeLongKey(name, lVal, comment),
                    bool bVal => new FreeBoolKey(name, bVal, comment),
                    Complex cVal => new FreeComplexKey(name, cVal, comment),
                    string sVal => new FreeStringKey(name, sVal, comment),
                    _ => throw new NotSupportedException(SR.KeyTypeNotSupported)
                };
    }
}