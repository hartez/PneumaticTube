# PneumaticTube

## Important Update!

Versions prior to 1.8 will stop working as of January 1, 2026 due to changes Dropbox is making to its infrastructure. You'll need to update to 1.8 or higher to continue using PneumaticTube. Version 1.8 bumps the required .NET version to .NET 10. 

If you're looking to update to 1.8 via [Chocolatey](https://chocolatey.org), the updated package is currently in moderation; I'll update this readme as soon as 1.8 is available.

## Command line Dropbox uploader for Windows

![Prague Pneumatic Post](http://upload.wikimedia.org/wikipedia/commons/thumb/f/fa/Hlavn%C3%AD-panel.jpg/320px-Hlavn%C3%AD-panel.jpg)

### Usage

`pneumatictube -f <file> -p <path>`

Uploads the specified file to the specified path in Dropbox. The `-f` option can also point to a folder, in which case each file in the folder will be uploaded to Dropbox. By default, only the files in the specified folder are uploaded; use the `-s` option to recursively upload the child folders.

For example:

`pneumatictube -f .\report.txt -p /docs` 

would upload `report.txt` to the `docs` folder in the Dropbox account.

### Options

* `-f` <file> (required) The location of the file to upload
* `-p` <path> The destination path in Dropbox (if left blank, will default to your Dropbox root)
* `-r` <reset> Force re-authorization with Dropbox
* `-c` <chunked> Force chunked uploading
* `-b` <bytes> Display progress in bytes instead of percentage when using chunked uploading
* `-q` <quiet> Suppress all output (except errors)
* `-n` <noprogress> Suppress progress reporting during chunked uploading
* `-k` <chunksize> Chunk size (in kilobytes) to use during chunked uploading
* `-t` <timeout> Timeout (in seconds) for HTTP connections
* `-s` <recursive> If the location to upload is a folder, recursively upload child folders

### Authorization

The first time you run PneumaticTube it will open a browser and ask you to authorize it for your Dropbox account.

If you ever want to deauthorize it (for example, to authorize it for a different account), you can run it with the `-r` (reset) option. 

### Chunked Uploading

Dropbox requires chunked uploading (uploading the file in many small parts, instead of one big blob) for files above 150 MB. Pneumatictube will automatically use chunked uploading for files which require it. For smaller files, you can specify the `-c` option to force chunked uploading. This is useful if you want a progress indicator during the upload. 

If you specify the `-c` option, you can also use the `-b` option to specify that you want your progress updates in bytes instead of percentage (the default), or `-n` to suppress progress reporting. 
  
The `-k` option allows you to specify the chunk size (in kilobytes) to use during chunked uploading. The default is 1024, and the minimum is 128. If you are uploading very large files, you may find a significant speed boost by increasing the chunk size so the file is uploaded in fewer chunks. 

### Installation

If you're not into building the project from source, you can download the [latest release](https://github.com/hartez/PneumaticTube/releases) as a `.zip`. Or, if you're a [chocolatey](https://chocolatey.org/) user, it's also available as a [package](https://chocolatey.org/packages/pneumatictube.portable). Just run `choco install pneumatictube.portable` and you should be good to go.

### Notes

This is built on the [.NET SDK for the Dropbox API v2](https://github.com/dropbox/dropbox-sdk-dotnet) and on [Command Line Parser](https://github.com/gsscoder/commandline). I basically just needed an easy way for a TeamCity server to push artifacts out to a Dropbox folder, and I didn't like all the awkward "run Dropbox as a service" hacks out there. 

-----

Image Credit:
By Serych at cs.wikipedia [Public domain], from Wikimedia Commons</a>
