$ErrorActionPreference = 'SilentlyContinue'
$p = Get-Process Unity -ErrorAction SilentlyContinue
if ($p) {
  $cpu = [int]$p.CPU
  $ram = [int]($p.WorkingSet64 / 1MB)
  Write-Output ("UNITY pid=" + $p.Id + " cpu_s=" + $cpu + " ram_mb=" + $ram)
} else {
  Write-Output 'UNITY_NOT_RUNNING'
}
foreach ($port in 8090, 8091) {
  $c = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
  if ($c) { Write-Output ("PORT_" + $port + "_LISTENING pid=" + $c.OwningProcess) }
  else { Write-Output ("PORT_" + $port + "_DOWN") }
}
