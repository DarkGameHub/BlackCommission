# Adds the existing (pip-installed) uv.exe directory to the USER PATH so
# Unity's "MCP for Unity" dependency check can find uv. Reuses the uv we
# already installed (uv 0.11.21), no re-download. User-scope only; does NOT
# touch the system PATH. ASCII-only on purpose (PowerShell 5.1 mis-decodes
# non-ASCII in BOM-less .ps1 files).

$dir = "C:\Users\yanfe\AppData\Roaming\Python\Python313\Scripts"

if (-not (Test-Path (Join-Path $dir 'uv.exe'))) {
    Write-Host "ERROR: uv.exe not found in $dir" -ForegroundColor Red
    Write-Host "Run 'python -m pip show uv' to find where uv installed." -ForegroundColor Yellow
    exit 1
}

$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
if (-not $userPath) { $userPath = '' }

if (($userPath -split ';') -contains $dir) {
    Write-Host "Already on user PATH: $dir" -ForegroundColor Cyan
} else {
    $newPath = ($userPath.TrimEnd(';') + ';' + $dir).TrimStart(';')
    [Environment]::SetEnvironmentVariable('Path', $newPath, 'User')
    Write-Host "ADDED to user PATH: $dir" -ForegroundColor Green
}

Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. FULLY quit Unity Editor AND Unity Hub (tray, Quit Unity Hub)."
Write-Host "  2. Reopen Hub, open the project."
Write-Host "  3. In MCP Setup, UV Package Manager should now be green."
