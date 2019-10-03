﻿using System;
using Maybe;

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
        bool TryFormat(Span<char> span);

        bool TryGetBytes(Span<byte> span);
    }

    public interface IFitsValue<T> : IFitsValue
    {
        Maybe<T> RawValue { get; }
    }
}
