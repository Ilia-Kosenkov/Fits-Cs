#nullable enable
using System;

namespace FitsCs
{
    public interface IFitsValue : IEquatable<IFitsValue>
    {
        string Name { get; }
        string Comment { get; }
        KeyType Type { get; }

        object? Value { get; }
        bool IsEmpty { get; }

        string ToString();
        string ToString(bool prefixType);
        bool TryFormat(Span<char> span);

        bool TryGetBytes(Span<byte> span);
    }

    public interface IFitsValue<T> : IFitsValue, IEquatable<IFitsValue<T>>
    {
        T RawValue { get; }
    }
}
