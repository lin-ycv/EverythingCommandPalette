# ECP-only resolver. ECP3 is deliberately a separate package.
param([Parameter(Mandatory)][string]$ReleaseTag)
$ErrorActionPreference = 'Stop'
$h = @{ 'User-Agent' = 'EverythingCmdPal-updater'; Accept = 'application/vnd.github+json' }
$tag = [uri]::EscapeDataString($ReleaseTag)
$r = Invoke-RestMethod -Uri "https://api.github.com/repos/lin-ycv/EverythingCommandPalette/releases/tags/$tag" -Headers $h
if ($r.draft -or $r.prerelease) { throw "Refusing draft/prerelease: $ReleaseTag" }
$pattern = '^VictorLin\.EverythingCP_(?<version>\d+\.\d+\.\d+\.\d+)_(?<arch>x64|arm64)__[^/]+\.msix$'
$a = @($r.assets | Where-Object { $_.name -match $pattern })
if ($a.Count -eq 0) {
    [pscustomobject]@{ Found = $false; ReleaseTag = $ReleaseTag } | ConvertTo-Json -Compress
    return
}
$x = @($a | Where-Object { $_.name -match '_x64__' })
$m = @($a | Where-Object { $_.name -match '_arm64__' })
if ($x.Count -ne 1 -or $m.Count -gt 1) {
    throw "Expected one ECP x64 and at most one ECP ARM64 MSIX (got x64=$($x.Count), ARM64=$($m.Count))."
}
$version = [regex]::Match($x[0].name, $pattern).Groups['version'].Value
if ($m.Count -eq 1 -and [regex]::Match($m[0].name, $pattern).Groups['version'].Value -ne $version) {
    throw 'ECP architectures have different versions.'
}
foreach ($asset in $a) {
    if ($asset.digest -notmatch '^sha256:[0-9a-fA-F]{64}$') { throw "Missing SHA256 for $($asset.name)." }
}
$installers = @([pscustomobject]@{
    Architecture = 'x64'
    Url = [string]$x[0].browser_download_url
    Sha256 = ([string]$x[0].digest).Substring(7).ToUpperInvariant()
})
if ($m.Count -eq 1) {
    $installers += [pscustomobject]@{
        Architecture = 'arm64'
        Url = [string]$m[0].browser_download_url
        Sha256 = ([string]$m[0].digest).Substring(7).ToUpperInvariant()
    }
}
[pscustomobject]@{
    Found = $true
    ReleaseTag = [string]$r.tag_name
    Version = $version
    Installers = $installers
} | ConvertTo-Json -Compress -Depth 5
