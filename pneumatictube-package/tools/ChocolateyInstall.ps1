$packageName = 'PneumaticTube.portable' 
$url = 'https://github.com/hartez/PneumaticTube/releases/download/v1.8/PneumaticTube.zip' 

$installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -Checksum "1405D4E7B18AD3E9143A91930778288C0582DEEC7D244A357309A62274D2ECE2" -ChecksumType sha256