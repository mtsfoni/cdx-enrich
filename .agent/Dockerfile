FROM ubuntu:22.04

RUN apt-get update && apt-get install -y \
    curl \
    git \
    ca-certificates \
    unzip \
    gnupg \
    build-essential \
    libicu-dev \
    lsb-release \
    && rm -rf /var/lib/apt/lists/*

RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs

#install copilot
RUN npm install -g @github/copilot

#install dotnet
RUN curl -fsSL https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet


#install docker
RUN mkdir -p /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
RUN apt-get update && apt-get install -y \
    docker-ce-cli \
    docker-buildx-plugin

RUN useradd -m agent
RUN groupadd -g 999 docker && usermod -aG docker agent


USER agent

# Create both possible config locations and ensure agent owns them
RUN mkdir -p /home/agent/.config/github-copilot /home/agent/.copilot
# Create the directory as root first
RUN mkdir -p /home/agent/.nuget/packages

# Configure git globally for the agent user
RUN git config --global user.email "agent@copilot.github.com" \
    && git config --global user.name "GitHub Copilot Agent"

WORKDIR /workspace
