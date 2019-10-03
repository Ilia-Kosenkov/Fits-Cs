using System;

namespace FitsCs
{
    public sealed class ArbitraryKey : FitsKey
    {
        private readonly string _contents;

        internal ArbitraryKey(string contents) : base(string.Empty, string.Empty)
        {
            if(string.IsNullOrWhiteSpace(contents) || contents.Length > EntrySize)
                throw new ArgumentException(SR.KeyValueTooLarge, nameof(contents));
            if(!contents.AsSpan().IsStringHduCompatible())
                throw new ArgumentException(SR.HduStringIllegal, nameof(contents));

            _contents = contents;
        }

        public override object Value => _contents;
        public override bool IsEmpty => string.IsNullOrWhiteSpace(_contents);
        public override bool TryFormat(Span<char> span)
        {
            if (span.Length < EntrySizeInBytes)
                return false;

            var exactSpan = span.Slice(0, _contents.Length);
            exactSpan.Fill(' ');
            _contents.AsSpan().CopyTo(exactSpan);
            return true;
        }
    }
}
