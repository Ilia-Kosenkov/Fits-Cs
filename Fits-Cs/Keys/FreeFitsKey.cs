using System;
using System.Numerics;
using JetBrains.Annotations;

namespace FitsCs.Keys
{
    public abstract class FreeFitsKey : FitsKey
    {
        private protected static readonly string EmptyString = string.Intern(new string (' ', 20));

        public override KeyType Type => KeyType.Free;
        private protected FreeFitsKey(string name, string comment) : base(name, comment)
        {
        }

        [NotNull]
        [ContractAnnotation("name:null => halt")]
        public static IFitsValue<T> Create<T>(string name, T value, string comment = null)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name), SR.NullArgument);
            
            // ReSharper disable AssignNullToNotNullAttribute
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
                case string sVal:
                    return new FreeStringKey(name, sVal, comment) as IFitsValue<T>;
            }
            // ReSharper restore AssignNullToNotNullAttribute
            
            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }

        [NotNull]
        [ContractAnnotation("name:null => halt;value:null => halt")]
        public static IFitsValue Create(string name, object value, string comment = null)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name), SR.NullArgument);
            if (value is null)
                throw new ArgumentNullException(nameof(value), SR.NullArgument);

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
                case string sVal:
                    return new FreeStringKey(name, sVal, comment);
            }

            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }
    }
}