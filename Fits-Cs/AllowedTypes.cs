using System;
using System.Collections.Immutable;
using System.Numerics;
using JetBrains.Annotations;

namespace FitsCs
{
    public static class AllowedTypes
    {
        public static ImmutableArray<Type> KeywordTypes { get; } = new[]
        {
            typeof(bool),
            typeof(int),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(Complex)
        }.ToImmutableArray();

        public static ImmutableArray<Type> DataTypes { get; } = new[]
        {
            typeof(int),
            typeof(float)
        }.ToImmutableArray();

        [PublicAPI]
        public static bool CanBeKeyType<T>() => KeywordTypes.Contains(typeof(T));

        [PublicAPI]
        [ContractAnnotation("type:null => false")]
        public static bool CanBeKeyType(Type type) => 
            !(type is null) && KeywordTypes.Contains(type);

        [PublicAPI]
        public static void ValidateKeyType<T>()
        {
            if (!CanBeKeyType<T>())
                throw new TypeAccessException(SR.KeyTypeNotSupported);
        }

        [PublicAPI]
        public static void ValidateKeyType(Type type)
        {
            if(!CanBeKeyType(type))
                throw new TypeAccessException(SR.KeyTypeNotSupported);
        }

        [PublicAPI]
        public static bool CanBeDataType<T>() => DataTypes.Contains(typeof(T));

        [PublicAPI]
        [ContractAnnotation("type:null => false")]
        public static bool CanBeDataType(Type type) =>
            !(type is null) && DataTypes.Contains(type);

        [PublicAPI]
        public static void ValidateDataType<T>()
        {
            if (!CanBeDataType<T>())
                throw new TypeAccessException(SR.DataTypeNotSupported);
        }

        [PublicAPI]
        public static void ValidateDataType(Type type)
        {
            if (!CanBeDataType(type))
                throw new TypeAccessException(SR.DataTypeNotSupported);
        }


    }
}
