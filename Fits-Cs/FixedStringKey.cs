using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Maybe;

namespace FitsCs
{
    public sealed class FixedStringKey : FixedFitsKey, IFitsValue<string>
    {
        internal FixedStringKey(string name, string comment) : base(name, comment)
        {
        }

        public override object Value => RawValue.Match(x => (object)x);
        public override bool IsEmpty => RawValue.Match(_ => true);
        public override bool TryFormat(Span<char> span, out int charsWritten)
        {
            
            

            throw new NotImplementedException(SR.Method_Not_Implemented);
        }
        public Maybe<string> RawValue { get; }

        internal FixedStringKey(string name, Maybe<string> value, string comment) : base(name, comment)
        {
            RawValue = value;
        }
    }
}
