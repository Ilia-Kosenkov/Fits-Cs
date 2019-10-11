using System;
using FitsCs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace Sandbox
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await Test1();
            //Test2();
        }

        private static async Task Test1()
        {

            using (var fs = new FileStream("FOCx38i0101t_c0f.fits", FileMode.Open))
            {
                using (var reader = new FitsReader(fs))
                {
                    var blob = await reader.ReadAsync();
                    var result = blob?.GetContentType();
                    if (result == BlobType.FitsHeader)
                    {
                        var keys = new List<IFitsValue>(36);
                        for (var i = 0; i < 36; i++)
                        {
                            keys.Add(FitsKey.ParseRawData(blob.Data.Slice(i * FitsKey.EntrySizeInBytes)));
                        }

                        foreach (var key in keys.Where(x => x is object))
                            Console.WriteLine(key.ToString(true));
                    }
                }
            }
        }


        private static void Test2()
        {
            var str = "very ''''specific'''' string y''all know";

            ParsingExtensions.TryParseRaw(str.AsSpan(), out string result);

        }
    }

}
