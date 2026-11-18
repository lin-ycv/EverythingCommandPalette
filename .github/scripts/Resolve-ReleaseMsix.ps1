# Resolve only original ECP (Everything SDK2), never ECP3.
# With no tag, find the latest published release containing ECP files.
param([string]$ReleaseTag)
$ErrorActionPreference = 'Stop'
$headers = @{ 'User-Agent' = 'EverythingCmdPal-updater'; Accept = 'application/vnd.github+json' }
if ($env:GITHUB_TOKEN) { $headers['Authorization'] = "Bearer $env:GITHUB_TOKEN" }
$pattern = '(?i)^VictorLin\.EverythingCP_(?<version>\d+\.\d+\.\d+\.\d+)_(?<arch>x64|arm64)__[^/]+\.msix$'

if ([string]::IsNullOrWhiteSpace($ReleaseTag)) {
    # Invoke-RestMethod returns GitHub's JSON array as one pipeline object.
    # Assign it, then foreach its elements; do NOT wrap the cmdlet in @(...).
    $releases = Invoke-RestMethod -Uri 'https://api.github.com/repos/lin-ycv/EverythingCommandPalette/releases?per_page=100' -Headers $headers
    $release = $null
    foreach ($candidate in $releases) {
        if ($candidate.draft -or $candidate.prerelease) { continue }
        foreach ($candidateAsset in $candidate.assets) {
            if ([string]$candidateAsset.name -match $pattern) {
                $release = $candidate
                break
            }
        }
        if ($null -ne $release) { break }
    }
    if ($null -eq $release) { throw 'No published release containing an ECP MSIX was found in the latest 100 releases.' }
} else {
    $tag = [uri]::EscapeDataString($ReleaseTag)
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/lin-ycv/EverythingCommandPalette/releases/tags/$tag" -Headers $headers
    if ($release.draft -or $release.prerelease) { throw "Refusing draft/prerelease: $ReleaseTag" }
}

$matchedAssets = @($release.assets | Where-Object { $_.name -match $pattern })
if ($matchedAssets.Count -eq 0) {
    # An explicit ECP3-only release is a normal skip, not an ECP failure.
    [pscustomobject]@{ Found = $false; ReleaseTag = [string]$release.tag_name } | ConvertTo-Json -Compress
    return
}
$x64 = @($matchedAssets | Where-Object { $_.name -match '_x64__' })
$arm64 = @($matchedAssets | Where-Object { $_.name -match '_arm64__' })
if ($x64.Count -ne 1 -or $arm64.Count -gt 1) {
    throw "Expected one ECP x64 and at most one ECP ARM64 MSIX (got x64=$($x64.Count), ARM64=$($arm64.Count))."
}
$version = [regex]::Match($x64[0].name, $pattern).Groups['version'].Value
if ($arm64.Count -eq 1) {
    $armVersion = [regex]::Match($arm64[0].name, $pattern).Groups['version'].Value
    if ($armVersion -ne $version) { throw "ECP architecture versions disagree: x64=$version ARM64=$armVersion." }
}
foreach ($asset in $matchedAssets) {
    if ([string]$asset.digest -notmatch '^sha256:[0-9a-fA-F]{64}$') { throw "Missing SHA256 for $($asset.name)." }
}
$installers = @([pscustomobject]@{
    Architecture = 'x64'
    Url = [string]$x64[0].browser_download_url
    Sha256 = ([string]$x64[0].digest).Substring(7).ToUpperInvariant()
})
if ($arm64.Count -eq 1) {
    $installers += [pscustomobject]@{
        Architecture = 'arm64'
        Url = [string]$arm64[0].browser_download_url
        Sha256 = ([string]$arm64[0].digest).Substring(7).ToUpperInvariant()
    }
}
[pscustomobject]@{
    Found = $true
    ReleaseTag = [string]$release.tag_name
    Version = $version
    Installers = $installers
} | ConvertTo-Json -Compress -Depth 5
