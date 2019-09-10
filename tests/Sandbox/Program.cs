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
            var key = new FixedIntKey("TEST", int.MinValue, "some comment");

            Span<char> span = new char[90];

            key.TryFormat(span, out var chars);

            var result = span.Slice(0, chars).ToString();
        }
    }
}
