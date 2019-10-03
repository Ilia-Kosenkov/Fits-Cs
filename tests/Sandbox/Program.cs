using FitsCs;
using Maybe;
using System;
using FitsCs.Keys;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            Test2();
        }

     
        private static void Test2()
        {
            var key = FitsKey.Create("NAME", "textsome''".Some(), "comment", KeyType.Free);
            Span<char> span = new char[90];

            key.TryFormat(span);
            var result = span.Slice(0, 80).ToString();


            var anotherKey = key.With(x => x.Value = (x.Value as Maybe<string>).Select(y => y.Length));

        }
    }

}
