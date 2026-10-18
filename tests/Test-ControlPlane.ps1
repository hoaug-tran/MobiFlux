$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$smokeRoot = Join-Path ([IO.Path]::GetTempPath()) ('mobiflux-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $smokeRoot | Out-Null
$reservation = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$reservation.Start()
$port = $reservation.LocalEndpoint.Port
$reservation.Stop()
$origin = "http://127.0.0.1:$port"
$previousStorage = $env:MOBIFLUX_Storage__DatabasePath
$previousUrls = $env:MOBIFLUX_urls
$env:MOBIFLUX_Storage__DatabasePath = Join-Path $smokeRoot 'smoke.db'
$env:MOBIFLUX_urls = $origin
$serviceDll = Join-Path $workspace 'src\MobiFlux.Service\bin\Debug\net10.0\MobiFlux.Service.dll'
$serviceDirectory = Join-Path $workspace 'src\MobiFlux.Service'
$smokeProcess = $null
try {
    $smokeProcess = Start-Process dotnet -ArgumentList @($serviceDll) -WorkingDirectory $serviceDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $smokeRoot 'stdout.log') -RedirectStandardError (Join-Path $smokeRoot 'stderr.log')
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        try { $health = Invoke-WebRequest "$origin/health" -TimeoutSec 1; $ready = $true; break }
        catch { Start-Sleep -Milliseconds 200 }
    }
    if (!$ready) { Get-Content (Join-Path $smokeRoot 'stdout.log'); throw 'Service did not become ready' }
    $api = "$origin/api/v1/mobiflux"
    $devices = Invoke-RestMethod "$api/devices"
    if (!$devices.succeeded -or $devices.data.Count -ne 0) { throw 'Unexpected device fixtures in isolated database' }
    $test = Invoke-RestMethod "$api/proxies/test-quick" -Method Post -ContentType 'application/json' -Body '{"name":"smoke","bindAddress":"127.0.0.1","port":1,"protocol":"Socks5"}'
    if (!$test.succeeded -or $test.data.reachable -or $test.data.resolvedWanIp) { throw 'Closed proxy incorrectly reported reachable' }
    $invalid = Invoke-WebRequest "$api/proxies/test-quick" -Method Post -ContentType 'application/json' -Body '{"name":"smoke","bindAddress":"8.8.8.8","port":1080,"protocol":"Socks5"}' -SkipHttpErrorCheck
    if ($invalid.StatusCode -ne 400) { throw 'Non-loopback proxy test must fail validation' }
    $fixture = Invoke-RestMethod "$api/devices" -Method Post -ContentType 'application/json' -Body '{"adbSerial":"smoke-no-physical-device","name":"Smoke fixture"}'
    $reservation.Start()
    $proxyPort = $reservation.LocalEndpoint.Port
    $reservation.Stop()
    $endpointBody = @{ name='Smoke endpoint'; bindAddress='127.0.0.1'; port=$proxyPort; protocol='Socks5'; deviceId=$fixture.data.id } | ConvertTo-Json
    $endpoint = Invoke-RestMethod "$api/proxies" -Method Post -ContentType 'application/json' -Body $endpointBody
    $endpointId = $endpoint.data.id
    Invoke-RestMethod "$api/proxy-runtime/$endpointId" -Method Put | Out-Null
    $openTest = Invoke-RestMethod "$api/proxies/$endpointId/test" -Method Post
    if (!$openTest.succeeded -or $openTest.data.reachable -or $openTest.data.resolvedWanIp) { throw 'Open listener without cellular incorrectly reported usable' }
    Invoke-RestMethod "$api/proxy-runtime/$endpointId" -Method Delete | Out-Null
    $updated = Invoke-RestMethod "$api/proxies/$endpointId" -Method Put -ContentType 'application/json' -Body '{"name":"Renamed endpoint","maxConnections":32,"enabled":true}'
    if ($updated.data.name -ne 'Renamed endpoint' -or $updated.data.maxConnections -ne 32) { throw 'Endpoint update was not persisted' }
    $poolBody = @{ name='Smoke pool'; strategy='RoundRobin'; stickyTtlSeconds=60; members=@(@{ deviceId=$fixture.data.id; weight=1; priority=0 }) } | ConvertTo-Json -Depth 5
    $pool = Invoke-RestMethod "$api/proxy-pools" -Method Post -ContentType 'application/json' -Body $poolBody
    $poolUpdate = @{ strategy='WeightedRoundRobin'; stickyTtlSeconds=120; enabled=$true; members=@(@{ deviceId=$fixture.data.id; weight=3; priority=1 }) } | ConvertTo-Json -Depth 5
    $poolSaved = Invoke-RestMethod "$api/proxy-pools/$($pool.data.id)" -Method Put -ContentType 'application/json' -Body $poolUpdate
    if ($poolSaved.data.members[0].weight -ne 3) { throw 'Pool weights were not persisted' }
    $rotation = Invoke-WebRequest "$api/rotation/rotate" -Method Post -ContentType 'application/json' -Body (@{ triggeredBy='Smoke'; deviceId=$fixture.data.id } | ConvertTo-Json) -SkipHttpErrorCheck
    if ($rotation.StatusCode -ne 409) { throw 'Offline selected device must not be rotated' }
    Invoke-RestMethod "$api/proxies/$endpointId" -Method Delete | Out-Null
    $remaining = Invoke-RestMethod "$api/proxies"
    if ($remaining.data.Count -ne 0) { throw 'Endpoint delete was not persisted' }
    & dotnet (Join-Path $workspace 'tests/MobiFlux.Tests/bin/Debug/net10.0/MobiFlux.Tests.dll') --hub $origin
    if ($LASTEXITCODE -ne 0) { throw 'Realtime SignalR verification failed' }
    Write-Output 'PASS: health, proxy fail-closed, endpoint update/delete, pool weights, offline rotation rejection and live SignalR'
    Write-Output "Smoke artifacts: $smokeRoot"
}
finally {
    if ($smokeProcess -and !$smokeProcess.HasExited) { Stop-Process -Id $smokeProcess.Id }
    $env:MOBIFLUX_Storage__DatabasePath = $previousStorage
    $env:MOBIFLUX_urls = $previousUrls
}
