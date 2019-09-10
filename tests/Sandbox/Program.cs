using System;
using FitsCs;

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
            var key = FitsKey.Create("TEST", 11, "Some comment");

            Span<char> span = new char[90];

            key.TryFormat(span, out var chars);

            var result = span.Slice(0, chars).ToString();

            var tr = result == key.ToString();

            Span<byte> bytes = new byte[128];

            key.TryGetBytes(bytes);
        }
    }
}
