using System;

namespace FitsCs
{
    public sealed class BlankKey : FitsKey
    {
        public static BlankKey Blank { get; } = new BlankKey();
        internal BlankKey() : base(string.Empty, string.Empty)
        {
        }

        public override object Value => null;
        public override bool IsEmpty => true;
        public override bool TryFormat(Span<char> span, out int charsWritten)
        {
            charsWritten = 0;

            if (span.Length < EntrySizeInBytes)
                return false;

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            charsWritten = EntrySizeInBytes;
            return true;
        }
    }
}
