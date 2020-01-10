using System;
using FitsCs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sandbox
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await Test2();
        }

        private static async Task Test1()
        {
            using var fs = new FileStream("WFPC2ASSNu5780205bx.fits", FileMode.Open);
            await using var reader = new FitsReader(fs);
            var keys = new List<IFitsValue>(36 * 3);
            var blob = await reader.ReadAsync();
            var readNext = true;
            while (readNext && blob?.GetContentType() == BlobType.FitsHeader)
            {
                for (var i = 0; i < 36; i++)
                {
                    var newKey = FitsKey.ParseRawData(blob.Data.Slice(i * FitsKey.EntrySizeInBytes));
                    keys.Add(newKey);
                }

                readNext = await reader.ReadAsync(blob);
            }


            for (var i = 0; i < keys.Count; i++)
            {
                Console.WriteLine(keys[i] is null
                    ? $"{i+1,3}\t### UNHANDLED ###"
                    : $"{i+1,3}\t{keys[i].ToString(true)}\t{keys[i].IsEmpty}");
            }
        }

        private static async Task Test2()
        {
            using var fs = new FileStream("DDTSUVDATA.fits", FileMode.Open);
            await using var reader = new FitsReader(fs);
            await foreach (var block in reader.EnumerateBlocksAsync())
            {
                foreach(var key in block.Keys)
                    Console.WriteLine(key.ToString());
            }
        }


    }

}
