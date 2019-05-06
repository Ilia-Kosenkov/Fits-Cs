//     MIT License
//     
//     Copyright(c) 2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Compatibility.Bridge;
using FitsCs;

namespace Tests
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                var name = @"WFPC2ASSNu5780205bx.fits";
                using (var fileStream = new FileStream(name, FileMode.Open, FileAccess.Read))
                    using (var reader = new FitsReader(fileStream, 1))
                    {
                        var blobs = await reader.ReadBlockAsync(23);

                        var keys = blobs.Where(x => x.TestIsKeywordsWeak()).SelectMany(x =>
                        {
                            var span = x.Memory.Span;
                            var collection = new List<FitsKey>(DataBlob.KeysPerBlob);
                            for (var i = 0; i < DataBlob.KeysPerBlob; i++)
                                collection.Add(FitsKey.Create(span.Slice(i * FitsKey.EntrySizeInBytes,
                                    FitsKey.EntrySizeInBytes)));
                            return collection;
                        }, (x, y) => y).ToImmutableList();

                        foreach (var item in keys.Where(x => !x.IsEmpty).Select(x => x is BoolFitsKey b
                            ? b.WithUpdates(y => y.Name = @"TEST")
                            : x))
                            Console.WriteLine(item.ToString(true));
                    }


            }
            catch (Exception e)
            {
                
            }

            return 0;
        }
    }
}
