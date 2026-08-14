FROM mcr.microsoft.com/devcontainers/dotnet

ARG HELIX_VERSION=25.07.1
ARG NETCOREDBG_VERSION=3.1.3-1062

USER root

# Install system tools.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends \
        ca-certificates \
        curl \
        git \
        ripgrep \
        fd-find \
        unzip \
        xz-utils \
    && ln -sf /usr/bin/fdfind /usr/local/bin/fd \
    && rm -rf /var/lib/apt/lists/*

# Install Helix.
RUN curl --fail --location \
        "https://github.com/helix-editor/helix/releases/download/${HELIX_VERSION}/helix-${HELIX_VERSION}-x86_64-linux.tar.xz" \
        --output /tmp/helix.tar.xz \
    && mkdir -p /opt/helix \
    && tar \
        --extract \
        --xz \
        --file=/tmp/helix.tar.xz \
        --directory=/opt/helix \
        --strip-components=1 \
    && ln -sf /opt/helix/hx /usr/local/bin/hx \
    && rm /tmp/helix.tar.xz

# Install the Microsoft Roslyn language server.
ENV PATH="/opt/dotnet-tools:${PATH}"

RUN mkdir -p /opt/dotnet-tools \
    && dotnet tool install \
        roslyn-language-server \
        --tool-path /opt/dotnet-tools \
        --prerelease \
    && chmod -R a+rX /opt/dotnet-tools \
    && chmod a+x "$(readlink -f /opt/dotnet-tools/roslyn-language-server)" \
    && test -x /opt/dotnet-tools/roslyn-language-server


  # Install Debugger
RUN curl --fail --location \
        "https://github.com/Samsung/netcoredbg/releases/download/${NETCOREDBG_VERSION}/netcoredbg-linux-amd64.tar.gz" \
        --output /tmp/netcoredbg.tar.gz \
    && mkdir -p /opt/netcoredbg \
    && tar \
        --extract \
        --gzip \
        --strip-components=1 \
        --file=/tmp/netcoredbg.tar.gz \
        --directory=/opt/netcoredbg \
    && chmod -R a+rX /opt/netcoredbg \
    && ln -sf /opt/netcoredbg/netcoredbg /usr/local/bin/netcoredbg \
    && test -x /usr/local/bin/netcoredbg \
    && rm /tmp/netcoredbg.tar.gz
  

# Create the Helix configuration directory.
RUN mkdir -p /home/vscode/.config/helix

# Create Helix's main config.
RUN cat <<'EOF' > /home/vscode/.config/helix/config.toml
theme = "dark_plus"

[editor]
line-number = "relative"
mouse = true
bufferline = "multiple"
cursorline = true
color-modes = true
auto-save = true
completion-trigger-len = 1
true-color = true
shell = ["bash", "-lc"]
end-of-line-diagnostics = "hint"

[editor.cursor-shape]
insert = "bar"
normal = "block"
select = "underline"

[editor.inline-diagnostics]
cursor-line = "warning"
other-lines = "error"

[editor.statusline]
left = [
  "mode",
  "spinner",
  "file-name",
  "read-only-indicator",
  "file-modification-indicator"
]

right = [
  "diagnostics",
  "selections",
  "position",
  "file-encoding"
]

[editor.lsp]
display-messages = true
display-inlay-hints = true
EOF

# Configure Roslyn and NetCoreDbg for C#.
RUN cat <<'EOF' > /home/vscode/.config/helix/languages.toml
[language-server.roslyn]
command = "roslyn-language-server"
args = [
  "--stdio",
  "--autoLoadProjects",
  "--telemetryLevel",
  "off"
]
timeout = 180

[[language]]
name = "c-sharp"
language-servers = ["roslyn"]
auto-format = true
roots = [
  "*.sln",
  "*.slnx",
  "*.csproj",
  "Directory.Build.props",
  "Directory.Build.targets",
  "global.json",
  ".git"
]

[language.debugger]
name = "netcoredbg"
transport = "tcp"
command = "netcoredbg"
args = ["--interpreter=vscode"]
port-arg = "--server={}"

[[language.debugger.templates]]
name = "Launch"
request = "launch"
completion = [
  { name = "path to dll", completion = "filename" }
]
args = { type = "coreclr", program = "{0}", cwd = ".", console = "internalConsole" }
EOF

# The files were created by root, so give the vscode user ownership.
RUN chown -R vscode:vscode /home/vscode/.config

USER vscode

WORKDIR /workspaces