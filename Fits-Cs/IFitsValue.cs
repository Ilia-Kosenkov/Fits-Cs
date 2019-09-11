using Maybe;
using System;
using System.Collections.Generic;
using System.Text;

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
        bool TryFormat(Span<char> span, out int charsWritten);

        bool TryGetBytes(Span<byte> span);
    }

    public interface IFitsValue<T> : IFitsValue
    {
        Maybe<T> RawValue { get; }
    }
}
