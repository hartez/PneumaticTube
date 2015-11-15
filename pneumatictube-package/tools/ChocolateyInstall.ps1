$packageName = 'PneumaticTube.portable' 
$url = 'https://github.com/hartez/PneumaticTube/releases/download/1.2/PneumaticTube.zip' 

$installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -md5 "ABAAA38826D87D63AC41CA0DDE104489"