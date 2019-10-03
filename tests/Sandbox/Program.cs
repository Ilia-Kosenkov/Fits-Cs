using FitsCs;
using Maybe;
using System;
using System.Numerics;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            Test2();
        }

        private static void Test1()
        {
            var key = FitsKey.Create("TEST", 11.0f.Some(), "Some comment");

            Span<char> span = new char[90];

            key.TryFormat(span);

            var result = span.Slice(0, 80).ToString();

            var tr = result == key.ToString();

            Span<byte> bytes = new byte[128];

            key.TryGetBytes(bytes);

            var anotherKey = FitsKey.Create<Complex>("CMPLX", new Complex(250, 0), "complex num");
            span.Fill('\0');
            anotherKey.TryFormat(span);
        }

        private static void Test2()
        {
            var key = FitsKey.Create("NAME", "textsome\r''".Some(), "comment", KeyType.Free);
            Span<char> span = new char[90];

            key.TryFormat(span);
            var result = span.Slice(0, 80).ToString();

         

        }
    }

}
