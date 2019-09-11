using System;

namespace FitsCs
{
    public sealed class ArbitraryKey : FitsKey
    {
        private readonly string _contents;

        internal ArbitraryKey(string contents) : base(string.Empty, string.Empty)
        {
            if(string.IsNullOrWhiteSpace(contents) || contents.Length > EntrySize)
                throw new ArgumentException(nameof(contents));

            _contents = contents;
        }

        public override object Value => _contents;
        public override bool IsEmpty => string.IsNullOrWhiteSpace(_contents);
        public override bool TryFormat(Span<char> span, out int charsWritten)
        {
            charsWritten = 0;
            if (span.Length < _contents.Length)
                return false;

            var exactSpan = span.Slice(0, _contents.Length);
            exactSpan.Fill(' ');
            _contents.AsSpan().CopyTo(exactSpan);
            charsWritten = _contents.Length;
            return true;
        }
    }
}
