using System;
using System.Collections.Generic;
using System.Text;

namespace FitsCs
{
    public interface IFitsValue<T>
    {
        string Name { get; }
        string Comment { get; }
        KeyType Type { get; }

        T RawValue { get; }

        string ToString();
        bool TryFormat(Span<char> span, out int charsWritten);
    }
}
