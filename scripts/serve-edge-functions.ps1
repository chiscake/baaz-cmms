# Edge Functions local dev on Windows (see AGENTS.md).
# `supabase functions serve` may fail with ENAMETOOLONG on Windows CLI 2.x.
# This script ensures supabase_edge_runtime_* is running and tails its logs.
#
# Note: if the edge_runtime container is missing, `supabase start` alone does not
# recreate it while the rest of the stack is up. We run `supabase stop` + `start`.
# Avoid `docker restart` on edge_runtime when Kong is up: Kong caches the container IP.

param(
    [string]$FunctionName = "admin-users",
    [string]$ProjectId = "baaz-cmms",
    [switch]$EnsureOnly
)

$ErrorActionPreference = "Stop"

$container = "supabase_edge_runtime_$ProjectId"
$kong = "supabase_kong_$ProjectId"

function Test-SupabaseStack {
    $db = docker ps -q -f "name=supabase_db_$ProjectId"
    if (-not $db) {
        Write-Error "Local Supabase is not running. Run: supabase start"
    }
}

function Ensure-EdgeRuntimeContainer {
    Write-Host "Edge runtime container missing. Restarting local Supabase stack..."
    supabase stop | Out-Null
    supabase start | Out-Null
    Start-Sleep -Seconds 3

    if (-not (docker ps -aq -f "name=$container")) {
        Write-Error "Edge runtime container was not created. Check: supabase start"
    }
}

function Restart-KongIfRunning {
    if (docker ps -q -f "name=$kong") {
        Write-Host "Restarting $kong so it picks up the edge runtime address..."
        docker restart $kong | Out-Null
        Start-Sleep -Seconds 5
    }
}

Test-SupabaseStack

$created = $false
$exists = docker ps -aq -f "name=$container"
if (-not $exists) {
    Ensure-EdgeRuntimeContainer
    $created = $true
}

$running = docker ps -q -f "name=$container"
if (-not $running) {
    Write-Host "Starting $container..."
    docker start $container | Out-Null
    Start-Sleep -Seconds 2
    Restart-KongIfRunning
}
elseif (-not $created) {
    Write-Host "$container is running (hot reload via per_worker policy)."
}

Start-Sleep -Seconds 2

if (-not (docker ps -q -f "name=$container")) {
    Write-Error "Failed to start $container. Check: docker logs $container"
}

Write-Host ""
Write-Host "Edge Functions: http://127.0.0.1:54321/functions/v1/$FunctionName"
Write-Host "Requires admin session JWT (not anon key)."

if ($EnsureOnly) {
    Write-Host "Edge runtime is ready."
    exit 0
}

Write-Host "Logs below (Ctrl+C to exit):"
Write-Host ""

docker logs -f $container
