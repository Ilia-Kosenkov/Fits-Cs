using System;
using System.Collections.Immutable;
using System.Numerics;

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
            typeof(byte),
            typeof(short),
            typeof(int),
            typeof(float),
            typeof(double)
        }.ToImmutableArray();

        public static bool CanBeKeyType<T>() => KeywordTypes.Contains(typeof(T));

        public static bool CanBeKeyType(Type type) =>
            !(type is null) && KeywordTypes.Contains(type);

        public static void ValidateKeyType<T>()
        {
            if (!CanBeKeyType<T>())
                throw new TypeAccessException(SR.KeyTypeNotSupported);
        }

        public static void ValidateKeyType(Type type)
        {
            if (!CanBeKeyType(type))
                throw new TypeAccessException(SR.KeyTypeNotSupported);
        }

        public static bool CanBeDataType<T>() => DataTypes.Contains(typeof(T));

        public static bool CanBeDataType(Type type) =>
            !(type is null) && DataTypes.Contains(type);

        public static void ValidateDataType<T>()
        {
            if (!CanBeDataType<T>())
                throw new TypeAccessException(SR.DataTypeNotSupported);
        }

        public static void ValidateDataType(Type type)
        {
            if (!CanBeDataType(type))
                throw new TypeAccessException(SR.DataTypeNotSupported);
        }
    }
}