# PneumaticTube

## Command line Dropbox uploader for Windows

![Prague Pneumatic Post](http://upload.wikimedia.org/wikipedia/commons/thumb/f/fa/Hlavn%C3%AD-panel.jpg/320px-Hlavn%C3%AD-panel.jpg)

### Usage

`pneumatictube -f <file> -p <path>`

Uploads the specified file to the specified path in Dropbox.

For example:

`pneumatictube -f .\report.txt -p /docs` 

would upload `report.txt` to the `docs` folder in the Dropbox account.

### Options

* `-f` <file> (required) The location of the file to upload
* `-p` <path> (required) The destination path in Dropbox
* `-r` <reset> Force re-authorization with Dropbox
* `-c` <chunked> Force chunked uploading
* `-b` <bytes> Display progress in bytes instead of percentage when using chunked uploading
* `-q` <quiet> Suppress all output (except errors)
* `-n` <noprogress> Suppress progress reporting during chunked uploading

### Authorization

The first time you run PneumaticTube it will open a browser and ask you to authorize it for your Dropbox account.

If you ever want to deauthorize it (for example, to authorize it for a different account), you can run it with the `-r` (reset) option. 

### Chunked Uploading

Dropbox requires chunked uploading (uploading the file in many small parts, instead of one big blob) for files above 150 MB. Pneumatictube will automatically use chunked uploading for files which require it. For smaller files, you can specify the `-c` option to force chunked uploading. This is useful if you want a progress indicator during the upload. 

If you specify the `-c` option, you can also use the `-b` option to specify that you want your progress updates in bytes instead of percentage (the default).

### Installation

If you're not into building the project from source, you can download the [latest release](https://github.com/hartez/PneumaticTube/releases) as a `.zip`. Or, if you're a [chocolatey](https://chocolatey.org/) user, it's also available as a [package](https://chocolatey.org/packages/pneumatictube.portable). Just run `choco install pneumatictube.portable` and you should be good to go.

### Notes

This is built on the excellent [DropNetRT](http://dropnet.github.io/dropnetrt.html) library and on [Command Line Parser](https://github.com/gsscoder/commandline). I basically just needed an easy way for a TeamCity server to push artifacts out to a Dropbox folder, and I didn't like all the awkward "run Dropbox as a service" hacks out there. 

-----

Image Credit:
By Serych at cs.wikipedia [Public domain], from Wikimedia Commons</a>
