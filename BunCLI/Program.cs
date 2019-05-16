using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using BunAPI;
using System.IO;
using System.Diagnostics;
using System.Net;
using HttpProgress;
using System.IO.Enumeration;
using System.Threading;

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

        private static readonly CancellationTokenSource shutdownCts = new CancellationTokenSource();

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                shutdownCts.Cancel();
            };

            // Read env vars.
            BaseOptions envOptions = new BaseOptions()
            {
                Zone = Environment.GetEnvironmentVariable(zoneEnvName),
                Key = Environment.GetEnvironmentVariable(keyEnvName)
            };

            try
            {
                // Parse arguments and run program.
                Parser.Default.ParseArguments<ListOptions, UploadOptions, DownloadOptions, DeleteOptions, SyncOptions>(args)
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
                case SyncOptions o:
                    Sync(client, o);
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
            if (result != HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"Could not delete file: The API returned HTTP status {result}.");
                Environment.Exit((int)ReturnCodes.OtherError);
            }
            else
            {
                Console.Error.WriteLine($"Deleted {o.Name}");
            }
        }

        private static void Upload(BunClient client, ITransferOptions o)
        {
            if (!Console.IsInputRedirected && o.LocalFilePath == null)
            {
                Console.Error.WriteLine("You must direct a stream to upload either by using pipes | or input redirection <.");
                Environment.Exit((int)ReturnCodes.ArgumentError);
            }
            else
            {
                string uploadName = o.RemoteName ?? "stdin";
                HttpStatusCode result;
                if (o.LocalFilePath != null)
                {
                    string localPath = o.LocalFilePath;
                    if (Directory.Exists(o.LocalFilePath))
                    {
                        localPath = Path.Combine(o.LocalFilePath, o.RemoteName);
                    }
                    else if (!File.Exists(o.LocalFilePath)) { Console.Error.WriteLine("$The specified file {o.FilePath} does not exist or isn't visible."); return; }
                    uploadName = o.RemoteName ?? Path.GetFileName(localPath);
                    using (var s = File.OpenRead(localPath))
                    {
                        Console.Error.WriteLine($"Uploading {uploadName}");
                        result = client.PutFile(s, uploadName, false, new Progress<ICopyProgress>((p) => { WriteProgress(p); })).Result;
                    }
                }
                else
                {
                    if (o.RemoteName == null) { Console.Error.WriteLine("The -n option must be used when uploading from stdin."); return; }
                    Console.Error.WriteLine("Uploading from stdin");
                    result = client.PutFile(Console.OpenStandardInput(), o.RemoteName, false, new Progress<ICopyProgress>((p) => { WriteProgress(p); })).Result;
                }

                if (result == HttpStatusCode.Created)
                {
                    Console.Error.WriteLine($"\nFile {uploadName} uploaded successfully.");
                }
                else
                {
                    Console.Error.WriteLine($"\nCould not complete upload: The API returned HTTP status {result}.");
                    Environment.Exit((int)ReturnCodes.OtherError);
                }
            }
        }

        private static void Download(BunClient client, ITransferOptions o)
        {
            Console.Error.WriteLine($"Downloading {o.RemoteName}");

            HttpStatusCode result;
            string destination = null;
            if (o is DownloadOptions options && options.StdOutFlag)
            {
                result = client.GetFile(o.RemoteName, Console.OpenStandardOutput(), new Progress<ICopyProgress>((p) => { WriteProgress(p); }), shutdownCts.Token).Result;
            }
            else
            {
                if (o.LocalFilePath == null) { o.LocalFilePath = Environment.CurrentDirectory; }
                if (Directory.Exists(o.LocalFilePath))
                {
                    destination = Path.Combine(o.LocalFilePath, o.RemoteName);
                }
                else
                {
                    destination = o.LocalFilePath;
                }
                using (var s = File.Create(destination))
                {
                    result = client.GetFile(o.RemoteName, s, new Progress<ICopyProgress>((p) => { WriteProgress(p); }), shutdownCts.Token).Result;
                    if (shutdownCts.IsCancellationRequested)
                    {
                        Console.Error.WriteLine("Download cancelled.");
                        s.Close();
                        if (File.Exists(destination))
                        {
                            File.Delete(destination);
                        }
                    }
                }
            }

            if (result != HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"\nCould not complete download: The API returned HTTP status {result}.");
                if (destination != null && File.Exists(destination))
                {
                    File.Delete(destination);
                }
                Environment.Exit((int)ReturnCodes.OtherError);
            }
            else
            {
                Console.Error.WriteLine($"\nDownload complete.");
            }
        }

        private static void Sync(BunClient client, SyncOptions o)
        {
            if (o.LocalPath == null) { o.LocalPath = Environment.CurrentDirectory; }
            if (!Directory.Exists(o.LocalPath))
            {
                Console.Error.WriteLine($"Specified local path doesn't exist: {o.LocalPath}.");
                Environment.Exit((int)ReturnCodes.ArgumentError);
            }

            Direction direction = Direction.None;
            switch (o.Direction.ToLowerInvariant())
            {
                case "up":
                    direction = Direction.Up;
                    break;
                case "down":
                    direction = Direction.Down;
                    break;
                default:
                    Console.Error.WriteLine($"Invalid direction: {o.Direction}. Only 'up' and 'down' are accepted.");
                    Environment.Exit((int)ReturnCodes.ArgumentError);
                    break;
            }

            // Get remote listing.
            Console.Error.WriteLine("Fetching remote listing...");
            var rawRemote = client.ListFiles().Result;
            if (rawRemote.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"Could not fetch remote listing for sync: HTTP status {rawRemote.StatusCode}.");
                Environment.Exit((int)ReturnCodes.OtherError);
            }
            var remoteFiles = rawRemote.Files.Where(x => !x.IsDirectory).Select(x => new SyncFile(Path.Combine(x.Path.Substring(x.StorageZoneName.Length + 2), x.ObjectName), x.LastChanged, x.Length));
            if (shutdownCts.IsCancellationRequested) { return; }

            // Get local listing.
            var enumerationOptions = new EnumerationOptions() { RecurseSubdirectories = true, IgnoreInaccessible = true };
            var localFiles = new FileSystemEnumerable<(string Path, DateTimeOffset lastModified, long length)>(o.LocalPath,
                (ref FileSystemEntry entry) => (entry.ToFullPath().Substring(o.LocalPath.Length + 1), entry.LastWriteTimeUtc, entry.Length), enumerationOptions)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => { return !entry.IsDirectory; }
            }.Select(x => new SyncFile(x.Path, x.lastModified, x.length));
            if (shutdownCts.IsCancellationRequested) { return; }

            switch (direction)
            {
                case Direction.Up:
                    var toUpload = CompareSets(localFiles, remoteFiles);
                    PerformSync(Upload, client, o, toUpload);
                    break;
                case Direction.Down:
                    var toDownload = CompareSets(remoteFiles, localFiles);
                    PerformSync(Download, client, o, toDownload);
                    break;
                default:
                    throw new Exception("Direction unspecified.");
            }
        }

        private static void PerformSync(Action<BunClient, ITransferOptions> method, BunClient client, SyncOptions o, List<SyncFile> files)
        {
            int i = 1;
            if (files.Count == 0)
            {
                Console.Error.WriteLine($"Nothing to sync.");
                return;
            }
            foreach (var file in files)
            {
                Console.Error.WriteLine($"{i}/{files.Count}");
                try
                {
                    method.Invoke(client, new DownloadOptions()
                    {
                        Zone = o.Zone,
                        Key = o.Key,
                        LocalFilePath = o.LocalPath,
                        RemoteName = file.Path
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to transfer {file.Path}: {ex.Message}");
                }
                i++;
                if (shutdownCts.IsCancellationRequested) { return; }
            }
        }

        private static List<SyncFile> CompareSets(IEnumerable<SyncFile> sourceSet, IEnumerable<SyncFile> baseSet)
        {
            var toDownload = new List<SyncFile>();
            var baseHashSet = baseSet.ToHashSet();
            foreach (var sourceFile in sourceSet)
            {
                if (baseHashSet.TryGetValue(sourceFile, out SyncFile localFile))
                {
                    if (localFile.Length == sourceFile.Length && localFile.LastModified >= sourceFile.LastModified)
                    {
                        continue;
                    }
                }
                toDownload.Add(sourceFile);
            }
            return toDownload;
        }

        private static void List(BunClient client)
        {
            var result = client.ListFiles().Result;
            if (result.StatusCode != HttpStatusCode.OK)
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

        private static Stopwatch lastUpdated = new Stopwatch();
        private static long lastTransfered = 0;
        private const int refreshRate = 333;
        private static void WriteProgress(ICopyProgress progress)
        {
            if (!lastUpdated.IsRunning)
            {
                lastUpdated.Start();
                return;
            }
            else
            {
                if (lastUpdated.ElapsedMilliseconds < refreshRate && progress.PercentComplete != 1) { return; }
            }

            int transferRate = (int)((progress.BytesTransferred - lastTransfered) / Math.Max(refreshRate, lastUpdated.ElapsedMilliseconds) * 1000);
            lastTransfered = progress.BytesTransferred;
            lastUpdated.Restart();

            string progressBar = $"[{string.Concat(Enumerable.Repeat('#', (int)Math.Floor(progress.PercentComplete * 20))).PadRight(20)}]";
            string transfered = FilesizeFormatter.FormatFilesize(progress.BytesTransferred);
            string expected = FilesizeFormatter.FormatFilesize(progress.ExpectedBytes);
            string rate = FilesizeFormatter.FormatFilesize(transferRate);
            string transferRatio = $"{ transfered } / { expected}";
            string progressLine = $"\r{progress.PercentComplete.ToString("P").PadLeft(8)} {progressBar} {transferRatio} @ {rate}/s".PadRight(80);
            Console.Error.Write(progressLine);

            if (progress.PercentComplete == 1)
            {
                lastTransfered = 0;
                lastUpdated.Stop();
            }
        }
    }
}
