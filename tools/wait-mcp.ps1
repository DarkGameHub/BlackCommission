$ErrorActionPreference = 'SilentlyContinue'
for ($i = 0; $i -lt 24; $i++) {
  $c = Get-NetTCPConnection -LocalPort 8090 -State Listen -ErrorAction SilentlyContinue
  if ($c) { Write-Output 'PORT_8090_LISTENING'; exit 0 }
  Start-Sleep -Seconds 10
}
Write-Output 'PORT_NOT_UP_YET'
exit 1
