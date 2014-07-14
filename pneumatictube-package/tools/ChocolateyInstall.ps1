$packageName = 'PneumaticTube.portable' 
$url = 'https://github.com/hartez/PneumaticTube/releases/download/1.0.0/PneumaticTube.zip' 

try { 
  $binRoot = "$env:systemdrive\tools"
  if($env:chocolatey_bin_root -ne $null -and $env:chocolatey_bin_root -notlike '*:\*'){$binRoot = join-path $env:systemdrive $env:chocolatey_bin_root}
  $installDir = Join-Path $binRoot "$packageName"
  Write-Host "Adding `'$installDir`' to the path and the current shell path"
  Install-ChocolateyPath "$installDir"
  $env:Path = "$($env:Path);$installDir"
 
  Install-ChocolateyZipPackage "$packageName" "$url" "$installDir"
 
  Write-ChocolateySuccess "$packageName"
} catch {
  Write-ChocolateyFailure "$packageName" "$($_.Exception.Message)"
  throw 
}