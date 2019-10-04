using FitsCs;
using Maybe;
using System;
using System.IO;
using System.Threading.Tasks;
using FitsCs.Keys;

namespace Sandbox
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Test1();
            Test2();
        }

        private static async Task Test1()
        {

            using (var fs = new FileStream("FOCx38i0101t_c0f.fits", FileMode.Open))
            {
                using (var reader = new FitsReader(fs))
                {
                    var blob = await reader.ReadAsync();
                    var result = blob?.GetContentType();
                }
            }
        }


        private static void Test2()
        {
            //    var key = FitsKey.Create("NAME", "textsome''".Some(), "comment", KeyType.Free);
            //    Span<char> span = new char[90];

            //    key.TryFormat(span);
            //    var result = span.Slice(0, 80).ToString();


            //    var anotherKey = key.With(x => x.Value = (x.Value as Maybe<string>).Select(y => y.Length));

        }
    }

}
