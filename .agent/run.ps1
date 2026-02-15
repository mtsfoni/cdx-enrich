# Paths
$AGENT_DIR = $PSScriptRoot
$REPO_ROOT = Resolve-Path "$AGENT_DIR\.."
$DOCKERFILE = "$AGENT_DIR\Dockerfile"
$ENV_FILE   = "$AGENT_DIR\.env"
$IMAGE_NAME = "copilot-dotnet"

# 0. Cleanup old sessions
Write-Host "🧹 Cleaning up old sessions..." -ForegroundColor Gray
docker rm -f agent-session 2>$null

# 1. Rebuild if needed (Docker handles the "is it changed?" logic via caching)
Write-Host "🛠️ Checking for environment updates..." -ForegroundColor Gray
docker build -t $IMAGE_NAME -f $DOCKERFILE $AGENT_DIR

# 2. Check if .env exists
if (-not (Test-Path $ENV_FILE)) {
    Write-Host "❌ Error: .env file not found at $ENV_FILE" -ForegroundColor Red
    Write-Host "Create it with: GH_TOKEN=ghp_your_token"
    exit
}

# 2. Ensure shared docker-daemon is running
Write-Host "🐳 Ensuring docker-daemon is running..." -ForegroundColor Gray

$daemonExists = docker ps -a --format "{{.Names}}" | Where-Object { $_ -eq "docker-daemon" }
$daemonRunning = docker ps --format "{{.Names}}" | Where-Object { $_ -eq "docker-daemon" }

if (-not $daemonExists) {
    Write-Host "➕ Creating docker-daemon..." -ForegroundColor Gray
    docker run -d `
        --name docker-daemon `
        --privileged `
        -e DOCKER_TLS_CERTDIR="" `
        -v dind-cache:/var/lib/docker `
        docker:dind
}
elseif (-not $daemonRunning) {
    Write-Host "▶️ Starting existing docker-daemon..." -ForegroundColor Gray
    docker start docker-daemon
}
else {
    Write-Host "✅ docker-daemon already running." -ForegroundColor Gray
}

# 3. Launch
Write-Host "🚀 Launching Agent at: $REPO_ROOT" -ForegroundColor Cyan
docker run -it --rm `
    --env-file "$ENV_FILE" `
    -v "${REPO_ROOT}:/workspace" `
    -v "${AGENT_DIR}/config.json:/home/agent/.copilot/config.json" `
    -v "${AGENT_DIR}/agent-instructions.md:/workspace/.github/copilot-instructions.md" `
    -v "nuget-cache:/home/agent/.nuget/packages" `
    --link docker-daemon:docker-daemon `
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 `
    -e DOCKER_HOST="tcp://docker-daemon:2375" `
    -w /workspace `
    --name agent-session `
    $IMAGE_NAME copilot --yolo