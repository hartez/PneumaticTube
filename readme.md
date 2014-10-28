# PneumaticTube

## Command line Dropbox uploader for Windows

![Prague Pneumatic Post](http://upload.wikimedia.org/wikipedia/commons/thumb/f/fa/Hlavn%C3%AD-panel.jpg/320px-Hlavn%C3%AD-panel.jpg)

### Usage

`pneumatictube -f <file> -p <path>`

Uploads the specified file to the specified path in Dropbox.

### Authorization

The first time you run PneumaticTube it will open a browser and ask you to authorize it for your Dropbox account.

If you ever want to deauthorize it (for example, to authorize it for a different account), you can run it with the `-r` (reset) option. 

### Installation

If you're not into building the project from source, you can download the latest release [as a .zip](https://github.com/hartez/PneumaticTube/releases/download/1.0.2/PneumaticTube.zip). Or, if you're a [chocolatey](https://chocolatey.org/) user, it's also available as a [package](https://chocolatey.org/packages/pneumatictube.portable). Just run `choco install pneumatictube.portable` and you should be good to go.


### Notes

This is built on the excellent [DropNet](http://dkdevelopment.net/what-im-doing/dropnet/) library and on [Command Line Parser](https://github.com/gsscoder/commandline). I basically just needed an easy way for a TeamCity server to push artifacts out to a Dropbox folder, and I didn't like all the awkward "run Dropbox as a service" hacks out there. 

-----

Image Credit:
By Serych at cs.wikipedia [Public domain], from Wikimedia Commons</a>
