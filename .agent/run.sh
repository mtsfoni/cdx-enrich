#!/usr/bin/env zsh

# Paths
AGENT_DIR="${0:A:h}"
REPO_ROOT="${AGENT_DIR:h}"
DOCKERFILE="$AGENT_DIR/Dockerfile"
ENV_FILE="$AGENT_DIR/.env"
IMAGE_NAME="copilot-dotnet"

# 0. Cleanup old sessions
echo "🧹 Cleaning up old sessions..."
docker rm -f agent-session 2>/dev/null

# 1. Rebuild if needed (Docker handles the "is it changed?" logic via caching)
echo "🛠️ Checking for environment updates..."
docker build -t $IMAGE_NAME -f $DOCKERFILE $AGENT_DIR

# 2. Check if .env exists
if [[ ! -f "$ENV_FILE" ]]; then
  echo "❌ Error: .env file not found at $ENV_FILE"
  echo "Create it with: GH_TOKEN=ghp_your_token"
  exit 1
fi

# 3. Ensure shared docker-daemon is running
echo "🐳 Ensuring docker-daemon is running..."
daemon_exists=$(docker ps -a --format "{{.Names}}" | grep -x "docker-daemon")
daemon_running=$(docker ps --format "{{.Names}}" | grep -x "docker-daemon")

if [[ -z "$daemon_exists" ]]; then
  echo "➕ Creating docker-daemon..."
  docker run -d \
    --name docker-daemon \
    --privileged \
    -e DOCKER_TLS_CERTDIR="" \
    -v dind-cache:/var/lib/docker \
    docker:dind
elif [[ -z "$daemon_running" ]]; then
  echo "▶️ Starting existing docker-daemon..."
  docker start docker-daemon
else
  echo "✅ docker-daemon already running."
fi

# 4. Launch
echo "🚀 Launching Agent at: $REPO_ROOT"
docker run -it --rm \
  --env-file "$ENV_FILE" \
  -v "${REPO_ROOT}:/workspace:z" \
  -v "${AGENT_DIR}/config.json:/home/agent/.copilot/config.json:z" \
  -v "nuget-cache:/home/agent/.nuget/packages" \
  --link docker-daemon:docker-daemon \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  -e DOCKER_HOST="tcp://docker-daemon:2375" \
  -w /workspace \
  --name agent-session \
  $IMAGE_NAME copilot --yolo