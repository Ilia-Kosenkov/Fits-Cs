# Fits-Cs
`Fits-Cs` is an attempt to bring [`FITS`](https://fits.gsfc.nasa.gov/) support to `.NET` environment. 
Currently, `NASA` [lists](https://fits.gsfc.nasa.gov/fits_libraries.html) only two `.NET` libraries written in `C#`, one of which is apparently unavailable, another - was updated [in 2003](http://skyservice.pha.jhu.edu/develop/Fitslib/).

In the era of `.NET Core`, there should be a better parsing option.

## What this project is about?
This library is an updated version of the custom `FITS` IO lib that is used with the `DIPOL-UF` optical polarimeter. While polarimeter requires only basic support of (uncompressed) single-unit (i.e. no extensions) image support, it is good to have universal tool (or at least as flexible as possible) to deal with `FITS`.
While `FITS` [standard](https://fits.gsfc.nasa.gov/fits_standard.html) is quite complicated, the majority of the features are intended to be supported.

## What technologies?
Because its predecessor was written before `.NET Core` maturity, it is essential to support `.NET Standard 2.0`, which is compatible with `.NET Framework 4.8` and `.NET Core 2.0`. The project is targeting both standard `2.0` and `2.1` as `2.1` has much, much more features available.

In order to make this library as fast and versatile as I can, I utilize the latest additions to the `C#` compiler and `BCL`, including `Span<T>` for allocation free buffer management, [`ValueTask<T>`](https://www.nuget.org/packages/System.Threading.Tasks.Extensions/) to power `async` IO and so on. Due to limited availability of some of the new APIs in standard `2.0`, I heavily rely on a number of custom extension/helper methods, see [Compatibility](https://github.com/Ilia-Kosenkov/Compatibility) project.

## The concept
`Fits-Cs` is built around `FitsReader`/`FitsWriter` classes, which mimic the role of `System.IO.StreamReader/StreamWriter`.
The stream is read as a sequence of 2880-bytes [**blobs**](Fits-Cs/DataBlob.cs), and a valid sequence of blobs can be converted into a [**data block**](Fits-Cs/Block.cs), which stores header key collection, information about the data types and size of the data array and a buffer containing all the data corresponding to this unit. Another library will be used to parse this segmented data blocks into images/tables/other formats.

To send data to a stream, it should be first converted to correct **data blocks**, which represent one valid `FITS` unit, which are then written as as sequence of 2880-bytes blobs.

## An example
```cs
// Assuming suport of C# 8.0
static async Task Example()
{
    var path = "path-to-some-fits.fits";
    using var fStr = new FileStream(path, FileMode.Open, FileAccess.Read);
    await using var fitsReader = new FitsReader(fStr);

    await foreach (var block in fitsReader.EnumerateBlocksAsync())
    {
        // block is a valid FITS unit, and reader enumerates through 
        // all valid units in the file
        foreach (var key in block.Keys)
            // Prints formatted key data with a data-type prefix
            // key in collection can be null only if parsing fails, 
            // which happens only if it does not conform to the standard
            Console.WriteLine(key?.ToString(true));
    }
}
```

To test the library, a set of *default* `FITS` [files](https://fits.gsfc.nasa.gov/fits_samples.html) provided by `NASA` is used. 
The goal is to be able to reasonable parse everything presented there, including 64-bit integers in main data array and keywords (implemented) and `CONTINUE` special keyword-extension to regular strings (halfway there). No support for non-standard characters in key names is planned. 

## How to start
The project relies on custom dependencies that are published as `NuGet` packages hosted on `github` right next to the repositories (see [configuration info](https://help.github.com/en/github/managing-packages-with-github-packages/configuring-dotnet-cli-for-use-with-github-packages)). If `github` source is added to the `NuGet` gallery, both `Fits-Cs` and its dependencies can be easily installed.


