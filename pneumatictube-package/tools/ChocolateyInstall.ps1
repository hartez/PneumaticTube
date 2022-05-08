$packageName = 'PneumaticTube.portable' 
$url = 'https://github.com/hartez/PneumaticTube/releases/download/v1.6/PneumaticTube.zip' 

$installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -md5 "C7343189A9C384F76B7E238139FFBF81"