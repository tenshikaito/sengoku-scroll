param([Parameter(Mandatory=$true)][string]$ReleasePath)
$ErrorActionPreference = 'Stop'
$releaseDirectory = (Resolve-Path -LiteralPath $ReleasePath).Path
$baseUri = 'http://127.0.0.1:5119'
$serverProcess = $null
function Start-TestServer {
    if (Get-NetTCPConnection -LocalPort 5119 -State Listen -ErrorAction SilentlyContinue) { throw 'Port 5119 is already in use; no requests were sent' }
    $script:serverProcess = Start-Process -FilePath (Join-Path $releaseDirectory 'SengokuScroll.WebApi.exe') -WorkingDirectory $releaseDirectory -WindowStyle Hidden -PassThru -ArgumentList @('--urls', $baseUri, '--Strategy:OpenBrowserOnStart=false', '--Strategy:DayDebug:Enabled=false', '--Strategy:DayDebug:WriteToFile=false') -RedirectStandardOutput (Join-Path $releaseDirectory 'smoke-server.log') -RedirectStandardError (Join-Path $releaseDirectory 'smoke-server-error.log')
    for ($attempt = 0; $attempt -lt 80; $attempt++) {
        if ($script:serverProcess.HasExited) { throw 'Test server exited during startup' }
        try { $null = Invoke-RestMethod "$baseUri/api/multiplayer/rooms" -TimeoutSec 1; return } catch { Start-Sleep -Milliseconds 250 }
    }
    throw 'Server did not start'
}
function Stop-TestServer {
    if ($script:serverProcess -and !$script:serverProcess.HasExited) {
        Stop-Process -Id $script:serverProcess.Id -Force
        $script:serverProcess.WaitForExit()
    }
}
function Send-Json($path, $body, $headers = @{}) {
    Invoke-RestMethod "$baseUri$path" -Method Post -ContentType 'application/json' -Body ($body | ConvertTo-Json -Depth 20 -Compress) -Headers $headers -TimeoutSec 30
}
try {
    Start-TestServer
    $page = Invoke-WebRequest $baseUri -TimeoutSec 5
    if ($page.StatusCode -ne 200) { throw 'Static client failed' }
    $created = Send-Json '/api/multiplayer/rooms' @{ roomName='restart-smoke'; playerName='host'; forceId=1; maxPlayers=2; difficulty='Easy' }
    $roomId = $created.room.roomId
    $guestForce = ($created.room.forces | Where-Object { !$_.occupied } | Select-Object -First 1).forceId
    $guest = Send-Json "/api/multiplayer/rooms/$roomId/join" @{ playerName='guest'; forceId=$guestForce }
    $headersOne = @{ 'X-Sengoku-Room-Id'=$roomId; 'X-Sengoku-Player-Token'=$created.credentials.playerToken; 'X-Sengoku-Command-Id'='one' }
    $headersTwo = @{ 'X-Sengoku-Room-Id'=$roomId; 'X-Sengoku-Player-Token'=$guest.credentials.playerToken; 'X-Sengoku-Command-Id'='two' }
    for ($day=0; $day -lt 3; $day++) {
        $headersOne['X-Sengoku-Command-Id'] = "one-$day"
        $headersTwo['X-Sengoku-Command-Id'] = "two-$day"
        $first = Send-Json "/api/multiplayer/rooms/$roomId/ready" @{ ready=$true; expectedTurn=$day } $headersOne
        $last = Send-Json "/api/multiplayer/rooms/$roomId/ready" @{ ready=$true; expectedTurn=$day } $headersTwo
        if ($first.advanced -or !$last.advanced -or $last.advance.events.Count -ne 0) { throw 'Ready isolation failed' }
    }
    $headersOne['X-Sengoku-Command-Id']='replay-proof'
    $null = Send-Json "/api/multiplayer/rooms/$roomId/ready" @{ ready=$false; expectedTurn=3 } $headersOne
    $before = Invoke-RestMethod "$baseUri/api/strategy/state" -Headers $headersOne
    $mailBefore = Invoke-RestMethod "$baseUri/api/multiplayer/rooms/$roomId/events" -Headers $headersOne
    if ($mailBefore.entries.Count -eq 0) { throw 'Expected private economic report' }
    Stop-TestServer
    Start-TestServer
    $restoredRoom = Invoke-RestMethod "$baseUri/api/multiplayer/rooms/$roomId"
    if ($restoredRoom.turnNumber -ne 3 -or ($restoredRoom.players | Where-Object { $_.connected -or $_.ready }).Count -gt 0) { throw 'Recovery presence/turn failed' }
    $null = Send-Json "/api/multiplayer/rooms/$roomId/reconnect" @{ playerId=$created.credentials.playerId; playerToken=$created.credentials.playerToken }
    $null = Send-Json "/api/multiplayer/rooms/$roomId/reconnect" @{ playerId=$guest.credentials.playerId; playerToken=$guest.credentials.playerToken }
    $after = Invoke-RestMethod "$baseUri/api/strategy/state" -Headers $headersOne
    if (($before | ConvertTo-Json -Depth 100 -Compress) -cne ($after | ConvertTo-Json -Depth 100 -Compress)) {
        $changed = @($before.PSObject.Properties | Where-Object {
            ($_.Value | ConvertTo-Json -Depth 100 -Compress) -cne ($after.($_.Name) | ConvertTo-Json -Depth 100 -Compress)
        } | ForEach-Object Name)
        for ($ci = 0; $ci -lt $before.characters.Count; $ci++) {
            $bc = $before.characters[$ci]; $ac = $after.characters | Where-Object { $_.id -eq $bc.id }
            foreach ($property in $bc.PSObject.Properties) {
                $beforeValue = $property.Value | ConvertTo-Json -Depth 100 -Compress
                $afterValue = $ac.($property.Name) | ConvertTo-Json -Depth 100 -Compress
                if ($beforeValue -cne $afterValue) { Write-Output "Character $($bc.id) $($property.Name): $beforeValue => $afterValue" }
            }
        }
        throw "State changed after restart: $($changed -join ', ')"
    }
    $mailAfter = Invoke-RestMethod "$baseUri/api/multiplayer/rooms/$roomId/events" -Headers $headersOne
    if (($mailBefore | ConvertTo-Json -Depth 100 -Compress) -cne ($mailAfter | ConvertTo-Json -Depth 100 -Compress)) { throw 'Private mailbox changed' }
    if (($mailAfter.entries | Where-Object { $_.event.recipientForceId -ne 1 }).Count -gt 0) { throw 'Recipient leakage' }
    $duplicate = Invoke-WebRequest "$baseUri/api/multiplayer/rooms/$roomId/ready" -Method Post -ContentType 'application/json' -Body '{"ready":false,"expectedTurn":3}' -Headers $headersOne -SkipHttpErrorCheck
    if ($duplicate.StatusCode -ne 409) { throw 'Dedup failed across restart' }
    $null = Send-Json "/api/multiplayer/rooms/$roomId/events/ack" @{ sequence=$mailAfter.lastSequence } $headersOne
    $emptyMail = Invoke-RestMethod "$baseUri/api/multiplayer/rooms/$roomId/events" -Headers $headersOne
    if ($emptyMail.entries.Count -ne 0) { throw 'Acknowledgement failed' }
    $null = Send-Json "/api/multiplayer/rooms/$roomId/leave" @{} $headersOne
    $null = Send-Json "/api/multiplayer/rooms/$roomId/leave" @{} $headersTwo
    if (Test-Path (Join-Path $releaseDirectory "App_Data/multiplayer-rooms/$roomId.json")) { throw 'Closed room snapshot retained' }
    [pscustomobject]@{ StaticClient='PASS'; TwoPlayerReady='PASS'; ProcessRestart='PASS'; CompletePlayerState='PASS'; PrivateMailbox='PASS'; PersistentDedup='PASS'; Acknowledgement='PASS'; LastLeaveCleanup='PASS' } | ConvertTo-Json
} finally { Stop-TestServer }
