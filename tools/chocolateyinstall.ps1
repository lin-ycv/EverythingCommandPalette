$ErrorActionPreference = 'Stop' # stop on all errors
#$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

Stop-Process -Name "EverythingCmdPal" -ErrorAction SilentlyContinue
$url = "https://github.com/lin-ycv/EverythingCommandPalette/releases/download/v_VERSION_/VictorLin.EverythingCP__VERSION__x64__yazqh14evg2ve.Msix"
$expectedHash = "_HASH64_"
$tempPath = $env:TEMP
$rnd = Get-Date -Format "yyyyMMdd"
$rnt = Get-Date -Format "HHmmss"
$newFileName = "ecp-$rnd.msix"
$destinationPath = Join-Path -Path $tempPath -ChildPath $newFileName

Get-ChildItem -Path "$tempPath\ecp-*.log" -ErrorAction SilentlyContinue | Remove-Item -Force
if(!(Test-Path $destinationPath)){
Get-ChildItem -Path "$tempPath\ecp-*.msix" -ErrorAction SilentlyContinue | Remove-Item -Force
Write-Host "File downloaded to $destinationPath"
if($env:PROCESSOR_ARCHITECTURE -eq "ARM64"){
        $url = "https://github.com/lin-ycv/EverythingCommandPalette/releases/download/v_VERSION_/VictorLin.EverythingCP__VERSION__ARM64__yazqh14evg2ve.Msix"
        $expectedHash = "_HASHarm64_"
}
Get-ChocolateyWebFile -PackageName 'Everything Command Palette' -FileFullPath "$destinationPath" -Url64 "$url" -Checksum64 "$expectedHash" -ChecksumType64 "sha256"
}else{Write-Host "Everything Command Palette installer already downloaded"}

#region WindowsAppRuntime
Write-Host "Checking Windows App Runtime"
$basePath = Join-Path ([System.Environment]::GetFolderPath('ProgramFiles')) "WindowsApps"
$pattern = "Microsoft\.WindowsAppRuntime\.\d+\.\d+_([0-9\.]+)_"
$minVersion = [version]"6000.401.2352.0"

$found = Get-ChildItem -Path $basePath -Directory -ErrorAction SilentlyContinue |
    Where-Object {
        if ($_.Name -match $pattern) {
            $ver = [version]$matches[1]
            return ($ver -ge $minVersion)
        }
        return $false
    }

if ($found){
    Write-Host "Windows App Runtime 1.6+ installed"
    Get-ChildItem -Path "$tempPath\WindowsAppRuntime166.exe" -ErrorAction SilentlyContinue | Remove-Item -Force
}
else {
    Write-Host "Windows App Runtime is NOT installed, installing package for $env:PROCESSOR_ARCHITECTURE... (This may take a while)"
    switch($env:PROCESSOR_ARCHITECTURE){
        "ARM64" {
            $url = "https://aka.ms/windowsappsdk/1.6/1.6.250228001/windowsappruntimeinstall-arm64.exe"
            $expectedHash = "F8CA545767445E3892E6130F1E050907727094F10D14AC9C9911475D5254AEFE"
        }
        "AMD64"{
            $url = "https://aka.ms/windowsappsdk/1.6/1.6.250228001/windowsappruntimeinstall-x64.exe"
            $expectedHash = "94A9561AE2E679D0D63EBA522C152E88A4EBAC157843E2B9BBEDBD308F285AE0"
        }
        default { throw "Unsupported architecture: $env:PROCESSOR_ARCHITECTURE, install terminated." }
    }
    $war = "WindowsAppRuntime166.exe"
    $destination = "$tempPath\$war"
    Get-ChocolateyWebFile -PackageName 'Windows App Runtime 1.6.6' -FileFullPath "$destination" -Url64 "$url" -Checksum64 "$expectedHash" -ChecksumType64 "sha256"
    & $destination --quiet
}
#endregion

Write-Host "Installing msix: $newFileName"
$command = "Add-AppxPackage `"$destinationPath`""
$erout = Join-Path -Path $tempPath -ChildPath "ecp-$rnd$rnt.log"
Start-Process powershell.exe `
  -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command `$command = `"$command`"; Invoke-Expression `$command 2>`'$($erout)'" `
  -Verb RunAs `
  -Wait
if((Get-Item $erout).Length -gt 0){ throw "Some error happened during install, see log: $erout" }
else{
    Get-ChildItem -Path "$tempPath\ecp-*.msix" -ErrorAction SilentlyContinue | Remove-Item -Force
    Get-ChildItem -Path "$tempPath\ecp-*.log" -ErrorAction SilentlyContinue | Remove-Item -Force
    Write-Host "Reminder: PowerToys/Command Palette and Everything needs to be installed separately" -ForegroundColor Yellow
}