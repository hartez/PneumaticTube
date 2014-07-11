$packageName = 'PneumaticTube' 
$installerType = 'MSI' 
$url = 'https://github.com/hartez/PneumaticTube/releases/download/1.0.0/PneumaticTube.Setup.msi' 
$silentArgs = '/quiet' 
$validExitCodes = @(0) 

Install-ChocolateyPackage "$packageName" "$installerType" "$silentArgs" "$url" -validExitCodes $validExitCodes
