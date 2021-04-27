#nullable enable
using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;

// WATCH : For testing purposes
[assembly:InternalsVisibleTo("Tests")]

namespace FitsCs
{
    public static class AllowedTypes
    {
        public static ImmutableArray<Type> AllowedKeyTypes { get; } =
            new[]
            {
                typeof(bool),
                typeof(int),
                typeof(long),
                typeof(float),
                typeof(double),
                typeof(Complex),
                typeof(string)

            }.ToImmutableArray();

        public static ImmutableArray<Type> AllowedDataTypes { get; } =
            new[]
            {
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(float),
                typeof(double)
            }.ToImmutableArray();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanBeKeyType<T>()
            => default(T) switch
            {
                bool or int or long or float or double or Complex => true,
                null when typeof(T) == typeof(string) => true,
                _ => false
            };

        public static bool CanBeKeyType(Type? type)
        {
            if (type is null)
                return false;
            if (type == typeof(bool))
                return true;
            if (type == typeof(int))
                return true;
            if (type == typeof(long))
                return true;
            if (type == typeof(float))
                return true;
            if (type == typeof(double))
                return true;
            if (type == typeof(Complex))
                return true;
            if (type == typeof(string))
                return true;

            return false;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanBeDataType<T>()
            => default(T) switch
            {
                byte or short or int or long or float or double => true,
                _ => false
            };

        public static bool CanBeDataType(Type? type)
        {
            if (type is null)
                return false;
            if (type == typeof(byte))
                return true;
            if (type == typeof(short))
                return true;
            if (type == typeof(int))
                return true;
            if (type == typeof(long))
                return true;
            if (type == typeof(float))
                return true;
            if (type == typeof(double))
                return true;
            
            return false;
        }

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