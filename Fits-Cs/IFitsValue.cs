using System;

namespace FitsCs
{
    public interface IFitsValue
    {
        string Name { get; }
        string Comment { get; }
        KeyType Type { get; }

        object Value { get; }
        bool IsEmpty { get; }

        string ToString();
        string ToString(bool prefixType);
        bool TryFormat(Span<char> span);

        bool TryGetBytes(Span<byte> span);
    }

    public interface IFitsValue<out T> : IFitsValue
    {
        T RawValue { get; }
    }
}
