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
        }

        private static async Task Test1()
        {

            using (var fs = new FileStream("FGSf64y0106m_a1f.fits", FileMode.Open))
            {
                using (var reader = new FitsReader(fs))
                {
                    var keys = new List<IFitsValue>(36 * 3);
                    var blob = await reader.ReadAsync();
                    while (blob?.GetContentType() == BlobType.FitsHeader)
                    {
                        for (var i = 0; i < 36; i++)
                        {
                            keys.Add(FitsKey.ParseRawData(blob.Data.Slice(i * FitsKey.EntrySizeInBytes)));
                        }


                        blob = await reader.ReadAsync();
                    }

                    for (var i = 0; i < keys.Count; i++)
                    {
                        Console.WriteLine(keys[i] is null 
                            ? $"{i:##0}    ### UNHANDLED ###" 
                            : $"{i:##0}\t{keys[i].ToString()}");
                    }
                }
            }
        }
        
    }

}
