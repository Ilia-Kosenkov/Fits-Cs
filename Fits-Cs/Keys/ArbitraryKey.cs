using JetBrains.Annotations;
using System;

namespace FitsCs.Keys
{
    public sealed class ArbitraryKey : FitsKey
    {
        private readonly string _contents;

        internal ArbitraryKey(string contents) : base(string.Empty, string.Empty)
        {
            if(string.IsNullOrWhiteSpace(contents) || contents.Length > EntrySize - EqualsPos)
                throw new ArgumentException(SR.KeyValueTooLarge, nameof(contents));
            if(!contents.AsSpan().IsStringHduCompatible())
                throw new ArgumentException(SR.HduStringIllegal, nameof(contents));

            _contents = contents;
        }

        private protected override string TypePrefix => @"arbtr";
        public override object Value => _contents;
        public override bool IsEmpty => string.IsNullOrWhiteSpace(_contents);
        public override bool TryFormat(Span<char> span)
        {
            if (span.Length < EntrySizeInBytes)
                return false;

            var exactSpan = span.Slice(0, EntrySize);
            exactSpan.Fill(' ');
            _contents.AsSpan().CopyTo(exactSpan.Slice(EqualsPos));
            return true;
        }

        [Pure]
        internal static bool IsArbitrary(ReadOnlySpan<char> input)
        {
            if (input.Length < EqualsPos + 1 
                || input.Length > EntrySize
                || !input.Slice(0, NameSize).IsWhiteSpace() 
                || input[EqualsPos] == '=')
                return false;

            return input.Slice(EqualsPos).IsStringHduCompatible();
        }

        [CanBeNull]
        internal static IFitsValue Create(ReadOnlySpan<char> input)
        {
            return IsArbitrary(input)
                ? new ArbitraryKey(input.Slice(EqualsPos).ToString())
                : null;
        }
    }
}
