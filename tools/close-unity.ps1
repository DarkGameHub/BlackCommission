$ErrorActionPreference = 'SilentlyContinue'
$procs = Get-Process Unity -ErrorAction SilentlyContinue
if (-not $procs) { Write-Output 'NO_UNITY'; exit 0 }
foreach ($p in $procs) { $p.CloseMainWindow() | Out-Null }
for ($i = 0; $i -lt 20; $i++) {
  Start-Sleep -Seconds 2
  if (-not (Get-Process Unity -ErrorAction SilentlyContinue)) { Write-Output 'CLOSED_CLEAN'; exit 0 }
}
# Still alive after 40s — force.
Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
if (-not (Get-Process Unity -ErrorAction SilentlyContinue)) { Write-Output 'CLOSED_FORCED' } else { Write-Output 'STILL_RUNNING' }
