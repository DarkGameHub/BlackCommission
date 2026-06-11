$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $projectRoot

$package = Get-ChildItem -Path (Join-Path $projectRoot 'Library/PackageCache') -Directory -Filter 'com.gamelovers.mcp-unity@*' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $package) {
    throw 'Could not find com.gamelovers.mcp-unity in Library/PackageCache.'
}

$env:UNITY_HOST = '127.0.0.1'
node (Join-Path $package.FullName 'Server~/build/index.js')
