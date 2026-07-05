# After `supabase db reset`, dependent containers get new IPs but Kong keeps stale upstreams.
# Restart Kong and wait until Auth responds before running seed scripts.

param(
    [string]$ProjectId = "baaz-cmms",
    [int]$MaxAttempts = 30,
    [int]$DelaySeconds = 2
)

$ErrorActionPreference = "Stop"

$kong = "supabase_kong_$ProjectId"
$authHealthUrl = "http://127.0.0.1:54321/auth/v1/health"

if (docker ps -q -f "name=$kong") {
    Write-Host "Restarting $kong after db reset (refresh upstream container IPs)..."
    docker restart $kong | Out-Null
    Start-Sleep -Seconds 3
}

for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
    try {
        $response = Invoke-WebRequest -Uri $authHealthUrl -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "Supabase Auth is ready."
            exit 0
        }
    }
    catch {
        # Auth or Kong may still be starting.
    }

    Write-Host "Waiting for Supabase Auth ($attempt/$MaxAttempts)..."
    Start-Sleep -Seconds $DelaySeconds
}

Write-Error "Supabase Auth did not become healthy within timeout."
