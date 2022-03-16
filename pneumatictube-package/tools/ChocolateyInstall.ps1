$packageName = 'PneumaticTube.portable' 
$url = 'https://github.com/hartez/PneumaticTube/releases/download/v1.5/PneumaticTube.zip' 

$installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -md5 "69BB1474F88B87269AA86AAB78AA251C"