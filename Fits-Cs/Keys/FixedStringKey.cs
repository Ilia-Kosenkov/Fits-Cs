#nullable enable
using System;

namespace FitsCs.Keys
{
    public sealed class FixedStringKey : FixedFitsKey, IFitsValue<string>
    {
        private protected override string TypePrefix => @"str";


        public override object Value => RawValue;
        public string RawValue { get; }
        public override bool TryFormat(Span<char> span)
        {
            var rawVal = RawValue.AsSpan();
            var n = rawVal.StringSizeWithQuoteReplacement() + 2;

            if (n > EntrySize)
                return false;

            Span<char> buff = stackalloc char[n];
            buff.Fill(' ');
            buff[0] = '=';

            if(!rawVal.TryGetCompatibleString(buff[2..]))
                throw new InvalidOperationException(SR.ShouldNotHappen);

            return TryFormat(span, buff);
        }

        internal FixedStringKey(string name, string value, string? comment) 
            : base(name, comment, value.AsSpan().StringSizeWithQuoteReplacement() + 2)
        {
            if (value is null)
                throw new ArgumentNullException(SR.NullArgument);
            
            if (!value.AsSpan().IsStringHduCompatible())
                throw new ArgumentException(SR.HduStringIllegal, nameof(value));
            RawValue = value;
        }

        public bool Equals(IFitsValue<string>? other)
            => other is { }
               && base.Equals(other)
               && RawValue == other.RawValue;

        public override bool Equals(IFitsValue? other)
            => other is IFitsValue<string> key
               && Equals(key);

        public override int GetHashCode()
            => unchecked((base.GetHashCode() * 397) ^ RawValue.GetHashCode());
    }
}
