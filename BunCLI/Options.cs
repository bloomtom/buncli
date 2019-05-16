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

    public interface ITransferOptions
    {
        string RemoteName { get; set; }
        /// <summary>
        /// The local path to download/upload. This can be a directory or file path, both possibilities should be accounted for.
        /// </summary>
        string LocalFilePath { get; set; }
    }

    public class BaseOptions : IBunOptions
    {
        [Option('z', "zone", Default = null, HelpText = "The storage zone.")]
        public string Zone { get; set; }

        [Option('k', "key", Default = null, HelpText = "Your API key for the desired storage zone.")]
        public string Key { get; set; }
    }
    
    public class TransferOptions : BaseOptions, ITransferOptions
    {
        [Option('n', "name", Default = null, HelpText = "The file name for the remote object, including any virtual paths.")]
        public string RemoteName { get; set; }

        [Option('p', "path", Default = null, HelpText = "The file path on local disk.")]
        public string LocalFilePath { get; set; }
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
    public class UploadOptions : TransferOptions
    {
        [Usage(ApplicationAlias = ">  dotnet bun.dll")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Upload a file from disk", new UploadOptions { Zone = "MyZone", Key = "01234567-89ab", LocalFilePath = "somefile.txt" });
            }
        }
    }

    [Verb("g", HelpText = "Get/download a file. The downloaded file is written to the standard output.")]
    public class DownloadOptions : TransferOptions
    {
        [Usage(ApplicationAlias = ">  dotnet bun.dll")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Download a file to the standard output stream", new DownloadOptions { Zone = "MyZone", Key = "01234567-89ab", RemoteName = "somefile.txt" });
            }
        }

        [Option('s', "stdout", Default = false, HelpText = "Download to standard output.")]
        public bool StdOutFlag { get; set; }
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

    [Verb("s", HelpText = "Synchronize files.")]
    public class SyncOptions : BaseOptions, IBunOptions
    {
        [Usage(ApplicationAlias = ">  dotnet bun.dll")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Download files not on the local system", new SyncOptions { Direction = "down" });
                yield return new Example("Upload files not on the remote", new SyncOptions { Direction = "up" });
            }
        }

        [Option('d', "direction", Required = true, Default = "down", HelpText = "The sync direction: up, down.")]
        public string Direction { get; set; }

        [Option('p', "path", Required = false, Default = null, HelpText = "The local path to sync against.")]
        public string LocalPath { get; set; }
    }
}