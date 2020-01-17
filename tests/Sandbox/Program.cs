using System;
using FitsCs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MemoryExtensions;

namespace Sandbox
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //await Test2();
            Test3();
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
            using var fs = new FileStream("DDTSUVDATA.fits", FileMode.Open, FileAccess.Read);
            await using var reader = new FitsReader(fs);

            using var ftarget = new FileStream("test.fits", FileMode.Create, FileAccess.Write);
            await using var writer = new FitsWriter(ftarget);

            await foreach (var block in reader.EnumerateBlocksAsync())
            {
                foreach(var key in block.Keys)
                    Console.WriteLine(key?.ToString(true));

                await writer.WriteBlockAsync(block);
            }

        }

        private static void Test3()
        {
            static ReadOnlySpan<char> LoremIpsum() =>
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
                    .AsSpan();

            var keys = FitsKey.ToContinueKeys(
                LoremIpsum(),
                LoremIpsum(),
                @"BOTH");

            var rec = FitsKey.ParseContinuedString(keys, true);

        }

    }

}
