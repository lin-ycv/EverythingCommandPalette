$ErrorActionPreference = 'Stop';

Write-Host "Uninstalling EverythingCmdPal"
#$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$tempPath = $env:TEMP
$rnd = Get-Date -Format "yyyyMMdd"
$rnt = Get-Date -Format "HHmmss"
Get-ChildItem -Path "$tempPath\ecp-*.msix" -ErrorAction SilentlyContinue | Remove-Item -Force
$erout = Join-Path -Path $tempPath -ChildPath "ecp-$rnd$rnt.log"
$command = "get-appxpackage VictorLin.EverythingCP* | remove-appxpackage"
Start-Process powershell.exe `
  -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command `$command = `"$command`"; Invoke-Expression `$command 2>`'$($erout)'" `
  -Verb RunAs `
  -Wait
if((Get-Item $erout).Length -gt 0){ throw "Some error happened during uninstall, see log: $erout" }
else{Get-ChildItem -Path "$tempPath\ecp-*.log" -ErrorAction SilentlyContinue | Remove-Item -Force}