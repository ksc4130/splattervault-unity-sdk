#!/usr/bin/env bash
set -euo pipefail

export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

# ── Check for .NET SDK ────────────────────────────────────────────
if ! command -v dotnet &>/dev/null; then
  echo ".NET SDK not found."
  read -rp "Would you like to install .NET 8 SDK now? [Y/n]: " INSTALL_INPUT
  INSTALL_INPUT="${INSTALL_INPUT:-Y}"
  if [[ "$INSTALL_INPUT" =~ ^[Yy] ]]; then
    echo "Downloading and installing .NET 8 SDK..."
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 8.0
    export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

    if ! command -v dotnet &>/dev/null; then
      echo "Error: Installation completed but 'dotnet' still not found."
      echo "Add this to your shell profile and restart your terminal:"
      echo "  export PATH=\"\$HOME/.dotnet:\$HOME/.dotnet/tools:\$PATH\""
      exit 1
    fi

    echo
    echo ".NET $(dotnet --version) installed successfully."
    echo
  else
    echo "Aborted. Install .NET 8 SDK manually: https://dot.net/install"
    exit 1
  fi
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
API_URL="https://splattervault.com/rest"

echo "╔══════════════════════════════════════════════════╗"
echo "║   SplatterVault SDK Integration Test Runner      ║"
echo "╚══════════════════════════════════════════════════╝"
echo
echo "What would you like to do?"
echo
echo "  1) Run test suite"
echo "  2) Start a session"
echo "  3) Check session status"
echo "  4) Stop a session"
echo
read -rp "Choice [1]: " CHOICE
CHOICE="${CHOICE:-1}"

# ── Prompt for API key ────────────────────────────────────────────
read -rp "API Key (sv_... or sv_org_...): " API_KEY
if [[ -z "$API_KEY" ]]; then
  echo "Error: API key is required."
  exit 1
fi

read -rp "API URL [${API_URL}]: " INPUT_URL
[[ -n "$INPUT_URL" ]] && API_URL="$INPUT_URL"

ARGS=(--api-url "$API_URL" --api-key "$API_KEY")

case "$CHOICE" in
  1)
    read -rp "Organization ID (optional, press Enter to skip): " ORG_ID
    [[ -n "$ORG_ID" ]] && ARGS+=(--org-id "$ORG_ID")

    read -rp "Skip destructive tests? (creates/stops real sessions) [Y/n]: " SKIP_INPUT
    SKIP_INPUT="${SKIP_INPUT:-Y}"
    [[ "$SKIP_INPUT" =~ ^[Yy] ]] && ARGS+=(--skip-destructive)

    echo
    echo "Running tests..."
    echo
    cd "$SCRIPT_DIR"
    dotnet run -- "${ARGS[@]}"
    ;;

  2)
    echo
    echo "Session configuration:"
    read -rp "  Session type (credit/subscription) [credit]: " SESSION_TYPE
    SESSION_TYPE="${SESSION_TYPE:-credit}"

    read -rp "  Game key [sys_1774636058786_30e0fc4d]: " GAME_KEY
    GAME_KEY="${GAME_KEY:-sys_1774636058786_30e0fc4d}"

    read -rp "  Region [NYC3]: " REGION
    REGION="${REGION:-NYC3}"

    read -rp "  Friendly name (optional): " FRIENDLY_NAME

    ARGS+=(--start-session --session-type "$SESSION_TYPE" --game-key "$GAME_KEY" --region "$REGION")
    [[ -n "$FRIENDLY_NAME" ]] && ARGS+=(--friendly-name "$FRIENDLY_NAME")

    echo
    cd "$SCRIPT_DIR"
    dotnet run -- "${ARGS[@]}"
    ;;

  3)
    ARGS+=(--session-status)
    echo
    cd "$SCRIPT_DIR"
    dotnet run -- "${ARGS[@]}"
    ;;

  4)
    ARGS+=(--stop-session)
    echo
    cd "$SCRIPT_DIR"
    dotnet run -- "${ARGS[@]}"
    ;;

  *)
    echo "Invalid choice."
    exit 1
    ;;
esac
