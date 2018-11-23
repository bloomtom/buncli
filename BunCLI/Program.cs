using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using BunAPI;
using System.IO;

namespace BunCLI
{
    public enum ReturnCodes : int
    {
        Success = 0,
        HelpPrinted = 1,
        ArgumentError = 2,
        OtherError = 3,
        Exception = 4
    }

    class Program
    {
        private const string zoneEnvName = "BUN_ZONE";
        private const string keyEnvName = "BUN_KEY";

        static void Main(string[] args)
        {
            // Read env vars.
            BaseOptions envOptions = new BaseOptions()
            {
                Zone = Environment.GetEnvironmentVariable(zoneEnvName),
                Key = Environment.GetEnvironmentVariable(keyEnvName)
            };

            try
            {
                // Parse arguments and run program.
                Parser.Default.ParseArguments<ListOptions, UploadOptions, DownloadOptions, DeleteOptions>(args)
                    .WithParsed<IBunOptions>(o =>
                    {
                        o.Key = o.Key ?? envOptions.Key;
                        o.Zone = o.Zone ?? envOptions.Zone;
                        RunWithOptions(o);
                    })
                    .WithNotParsed(e => HandleParseError(e));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit((int)ReturnCodes.Exception);
            }

            Environment.Exit((int)ReturnCodes.Success);
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            if (errs.Count() == 1)
            {
                var e = errs.First();
                if (e.Tag == ErrorType.VersionRequestedError || e.Tag == ErrorType.HelpRequestedError || e.Tag == ErrorType.HelpVerbRequestedError)
                {
                    Environment.Exit((int)ReturnCodes.HelpPrinted);
                }
            }

            Console.Error.WriteLine("Failed to parse command:");
            Console.Error.Write(string.Join('\n', errs));

            Environment.Exit((int)ReturnCodes.ArgumentError);
        }

        private static void RunWithOptions(IBunOptions commandOptions)
        {
            if (commandOptions.Key == null)
            {
                Console.Error.WriteLine($"Key not defined. Either pass the key as an argument or set the {keyEnvName} environment variable.");
                Environment.Exit((int)ReturnCodes.ArgumentError);
            }
            if (commandOptions.Zone == null)
            {
                Console.Error.WriteLine($"Zone not defined. Either pass the key as an argument or set the {zoneEnvName} environment variable.");
                Environment.Exit((int)ReturnCodes.ArgumentError);
            }

            var client = new BunClient(commandOptions.Key, commandOptions.Zone);

            // Handle the chosen verb.
            switch (commandOptions)
            {
                case ListOptions o:
                    List(client);
                    break;
                case UploadOptions o:
                    Upload(client, o);
                    break;
                case DownloadOptions o:
                    Download(client, o);
                    break;
                case DeleteOptions o:
                    Delete(client, o);
                    break;
                default:
                    Console.Error.WriteLine("Unknown verb.");
                    Environment.Exit((int)ReturnCodes.ArgumentError);
                    break;
            }
        }

        private static void Delete(BunClient client, DeleteOptions o)
        {
            var result = client.DeleteFile(o.Name).Result;
            if (result != System.Net.HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"Could not delete file: The API returned HTTP status {result}.");
                Environment.Exit((int)ReturnCodes.OtherError);
            }
            else
            {
                Console.Error.WriteLine($"Deleted {o.Name}");
            }
        }

        private static void Upload(BunClient client, UploadOptions o)
        {
            if (!Console.IsInputRedirected)
            {
                Console.Error.WriteLine("You must direct a stream to upload either by using pipes | or input redirection <.");
                Environment.Exit((int)ReturnCodes.ArgumentError);
            }
            else
            {
                var result = client.PutFile(Console.OpenStandardInput(), o.Name).Result;
                if (result != System.Net.HttpStatusCode.Created)
                {
                    Console.Error.WriteLine($"Could not complete upload: The API returned HTTP status {result}.");
                    Environment.Exit((int)ReturnCodes.OtherError);
                }
                else
                {
                    Console.Error.WriteLine($"File {o.Name} uploaded successfully.");
                }
            }
        }

        private static void Download(BunClient client, DownloadOptions o)
        {
            var result = client.GetFile(o.Name).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"Could not complete download: The API returned HTTP status {result.StatusCode}.");
                Environment.Exit((int)ReturnCodes.OtherError);
            }
            else
            {
                // Write as binary to stream.
                using (Stream binOut = Console.OpenStandardOutput())
                {
                    result.Stream.CopyTo(binOut);
                }
            }
        }

        private static void List(BunClient client)
        {
            var result = client.ListFiles().Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"Could not complete listing: The API returned HTTP status {result.StatusCode}.");
                Environment.Exit((int)ReturnCodes.OtherError);
            }
            else
            {
                const int padding = 14;
                Console.WriteLine(FormatListPrintLine(padding, "Created", "Size", "IsDir", "Name"));
                Console.WriteLine(string.Join('\n',
                    result.Files
                        .OrderBy(x => x.IsDirectory)
                        .ThenBy(x => x.ObjectName)
                        .Select(x =>
                        {
                            return FormatListPrintLine(padding,
                                        x.DateCreated.ToString("yyyy-MM-dd"),
                                        FilesizeFormatter.FormatFilesize(x.Length),
                                        x.IsDirectory.ToString())
                                        + x.Path + x.ObjectName;
                        })));
            }
        }

        private static string FormatListPrintLine(int padding, params string[] args)
        {
            return string.Concat(args.Select(x => x.PadRight(padding)));
        }
    }
}
