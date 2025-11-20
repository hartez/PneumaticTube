$packageName = 'PneumaticTube.portable' 
$url = 'https://github.com/hartez/PneumaticTube/releases/download/v1.8/PneumaticTube.zip' 

$installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -Checksum "E822557FA42175A4A580BDF8FE02B61649BF1BD97EC54A78E2CC130F773E70DA" -ChecksumType sha256