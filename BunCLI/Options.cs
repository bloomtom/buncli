using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace BunCLI
{
    public interface IBunOptions
    {
        string Zone { get; set; }
        string Key { get; set; }
    }

    public class BaseOptions : IBunOptions
    {
        [Option('z', "zone", Default = null, HelpText = "The storage zone.")]
        public string Zone { get; set; }

        [Option('k', "key", Default = null, HelpText = "Your API key for the desired storage zone.")]
        public string Key { get; set; }
    }

    [Verb("l", HelpText = "List files stored in a storage zone.")]
    public class ListOptions : BaseOptions, IBunOptions
    {
        [Usage(ApplicationAlias = ">  dotnet bun.dll")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("List all files in a storage zone", new ListOptions { Zone = "MyZone", Key = "01234567-89ab" });
            }
        }
    }

    [Verb("u", HelpText = "Upload a file from disk or stdin.")]
    public class UploadOptions : BaseOptions, IBunOptions
    {
        [Usage(ApplicationAlias = ">  dotnet bun.dll")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Upload a file from disk", new UploadOptions { Zone = "MyZone", Key = "01234567-89ab", FilePath = "somefile.txt" });
            }
        }

        [Option('n', "name", Default = null, HelpText = "The file name to upload as. Defaults to the filename on disk.")]
        public string Name { get; set; }

        [Option('p', "path", Default = null, HelpText = "The file path on disk.")]
        public string FilePath { get; set; }
    }

    [Verb("g", HelpText = "Get/download a file. The downloaded file is written to the standard output.")]
    public class DownloadOptions : BaseOptions, IBunOptions
    {
        [Usage(ApplicationAlias = ">  dotnet bun.dll")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Download a file to the standard output stream", new DownloadOptions { Zone = "MyZone", Key = "01234567-89ab", Name = "somefile.txt" });
            }
        }

        [Option('n', "name", Required = true, Default = null, HelpText = "The name of the file to download.")]
        public string Name { get; set; }

        [Option('d', "direct", Default = false, HelpText = "Download to disk in the current directory.")]
        public bool DirectDownloadFlag { get; set; }
    }

    [Verb("r", HelpText = "Remove/delete a file.")]
    public class DeleteOptions : BaseOptions, IBunOptions
    {
        [Usage(ApplicationAlias = ">  dotnet bun.dll")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Delete a file", new DeleteOptions { Zone = "MyZone", Key = "01234567-89ab", Name = "somefile.txt" });
            }
        }

        [Option('n', "name", Required = true, Default = null, HelpText = "The name of the file to delete.")]
        public string Name { get; set; }
    }
}