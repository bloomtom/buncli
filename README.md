# bun CLI
>A reference front-end for the [BunAPI](https://github.com/bloomtom/BunAPI) library.

This application is a CLI front-end for the unofficial [BunnyCDN](https://bunnycdn.com/) dotnet core library. Manage your storage zones with simple commands!

## Requirements
The only requirement is the dotnet core 2.1 runtime. This is installed by default on Windows 10, but on Linux or OSX you may need to install it.
A tutorial for installing dotnet using package management [can be found here](https://www.microsoft.com/net/learn/get-started-with-dotnet-tutorial). If you just want the binaries you can [find those here](https://www.microsoft.com/net/download/dotnet-core/2.1).

If you're not sure if you have the dotnet core runtime already, you can run this command:
```
dotnet --version
2.1.403
```
If the version is 2.1 or higher you're all set to go!

## Installation
Since this application is so small it's recommended you build it from source. The following commands should get you off the ground.

```
git clone https://github.com/bloomtom/buncli.git
cd BunCLI
dotnet build --configuration Release
dotnet BunCLI\bin\Release\netcoreapp2.1\bun.dll --help
```

## Command Options
The following is an output of the `--help` option.
```
>dotnet bun.dll --help
bun 0.1.0
MIT License - Copyright 2018 bloomtom

  l          List files stored in a storage zone.

  u          Upload a file. The file is read from the standard input.

  g          Get/download a file. The downloaded file is written to the standard output.

  r          Remove/delete a file.

  help       Display more information on a specific command.

  version    Display version information.
```
You can get more information about a command by asking for `--help` on a specific verb:
```
>dotnet bun.dll u --help
bun 0.1.0
MIT License - Copyright 2018 bloomtom
USAGE:
Upload a file from the standard input stream:
>  dotnet bun.dll u --key 01234567-89ab --name somefile.txt --zone MyZone

  -n, --name    The file name. Defaults to the filename on disk.

  -z, --zone    The storage zone.

  -k, --key     Your API key for the desired storage zone.

  --help        Display this help screen.

  --version     Display version information.
```
When running the upload or download command, data is taken and written to the standard input and output respectively. To upload a file you can use the `<` operator. The following will upload somefile.txt and name it MyFile.txt in your BunnyCDN storage.
```
dotnet bun.dll u -n MyFile.txt < somefile.txt
```
You can also use pipes. Forgive the UUOC for this example.
```
cat somefile.txt | dotnet bun.dll u -n MyFile.txt
```
Similarly, for downloading you use the `>` operator. The following will download MyFile.txt and write it into newfile.txt
```
dotnet bun.dll g -n MyFile.txt > newfile.txt
```

## Environment Variables

You may have noticed that the example commands don't specify an api key or storage zone, and they aren't specified as  required in the help screens. This is because you can use environment variables to set a default key and zone. The following will set these for your current shell session.
```
set BUN_ZONE=MyZone
set BUN_KEY=01234567-89ab...
```
If no environment variables are set for the zone or key, and they aren't specified in command options, an error will be thrown.
```
Key not defined. Either pass the key as an argument or set the BUN_KEY environment variable.
```
Command options always take priority over environment variables. So if you have a default zone set, you can override it by passing `-z`.