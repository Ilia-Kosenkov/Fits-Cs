using System;
using System.Numerics;
using FitsCs;
using Maybe;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            Test1();
        }

        private static void Test1()
        {
            var key = FitsKey.Create("TEST", 11.0f.Some(), "Some comment");

            Span<char> span = new char[90];

            key.TryFormat(span, out var chars);

            var result = span.Slice(0, chars).ToString();

            var tr = result == key.ToString();

            Span<byte> bytes = new byte[128];

            key.TryGetBytes(bytes);

            var anotherKey = FitsKey.Create<Complex>("CMPLX", new Complex(250,0 ), "complex num");
            span.Fill('\0');
            anotherKey.TryFormat(span, out chars);
        }
    }
}
