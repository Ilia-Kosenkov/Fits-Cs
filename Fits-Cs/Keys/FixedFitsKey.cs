using System;
using System.Numerics;
using JetBrains.Annotations;

namespace FitsCs.Keys
{
    public abstract class FixedFitsKey : FitsKey
    {
        public const int FixedFieldSize = 20;
        public override KeyType Type => KeyType.Fixed;
        public override bool IsEmpty => false;

        private protected FixedFitsKey(string name, string comment, int size) : base(name, comment, size)
        {
        }

        [NotNull]
        [ContractAnnotation("name:null => halt")]
        public static IFitsValue<T> Create<T>(string name, T value, string comment = null)
        {
            // Validation happens inside constructors
            //if (name is null)
            //    throw new ArgumentNullException(nameof(name), SR.NullArgument);

            // ReSharper disable AssignNullToNotNullAttribute
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
                case string sVal:
                    return new FixedStringKey(name, sVal, comment) as IFitsValue<T>;
            }
            // ReSharper restore AssignNullToNotNullAttribute


            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }

        [NotNull]
        [ContractAnnotation("name:null => halt;value:null => halt")]
        public static IFitsValue Create(string name, object value, string comment = null)
        {
            // Validation happens inside constructors
            //if (name is null)
            //    throw new ArgumentNullException(nameof(name), SR.NullArgument);
            if (value is null)
                throw new ArgumentNullException(nameof(value), SR.NullArgument);

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
                case string sVal:
                    return new FixedStringKey(name, sVal, comment);
            }

            throw new NotSupportedException(SR.KeyTypeNotSupported);
        }
    }
}