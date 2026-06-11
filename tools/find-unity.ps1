$ErrorActionPreference = 'SilentlyContinue'
$roots = @(
  'C:\Program Files\Unity\Hub\Editor',
  'C:\Program Files\Unity Hub\Editor',
  'D:\Unity\Hub\Editor',
  'D:\Program Files\Unity\Hub\Editor',
  'C:\Program Files\Unity\Editor'
)
$found = @()
foreach ($r in $roots) {
  if (Test-Path $r) {
    Get-ChildItem $r -Directory | ForEach-Object {
      $exe = Join-Path $_.FullName 'Editor\Unity.exe'
      if (Test-Path $exe) { $found += $exe }
    }
  }
}
if ($found.Count -eq 0) { 'NO_EDITOR_FOUND' } else { $found }
$hub = 'C:\Program Files\Unity Hub\Unity Hub.exe'
if (Test-Path $hub) { 'HUB_PRESENT' }
