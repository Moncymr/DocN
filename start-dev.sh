#!/bin/bash
# Development startup script for DocN
# This script starts both the Backend API (DocN.Server) and Frontend (DocN.Client) servers

echo "🚀 Starting DocN Development Environment..."
echo ""

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed."
    echo "Please install .NET 10.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "📦 Building projects..."
cd "$SCRIPT_DIR"

# Build both projects to ensure they're up to date
dotnet build DocN.Server/DocN.Server.csproj --no-incremental
if [ $? -ne 0 ]; then
    echo "❌ Failed to build DocN.Server"
    exit 1
fi

dotnet build DocN.Client/DocN.Client.csproj --no-incremental
if [ $? -ne 0 ]; then
    echo "❌ Failed to build DocN.Client"
    exit 1
fi

echo "✅ Build completed successfully!"
echo ""
echo "🌐 Starting servers..."
echo "   - Backend API: https://localhost:5211 (Ctrl+C in first terminal to stop)"
echo "   - Frontend UI: https://localhost:7114 (Ctrl+C in second terminal to stop)"
echo ""
echo "⚠️  IMPORTANT: You need to run this in two separate terminals:"
echo "   Terminal 1: cd DocN.Server && dotnet run"
echo "   Terminal 2: cd DocN.Client && dotnet run"
echo ""
echo "Or use the tmux/screen option below for a single terminal:"
echo ""

# Check if tmux is available
if command -v tmux &> /dev/null; then
    echo "Would you like to start both servers in a tmux session? (y/n)"
    read -r response
    if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        # Create a new tmux session with two panes
        tmux new-session -d -s docn -n servers
        tmux send-keys -t docn:servers "cd '$SCRIPT_DIR/DocN.Server' && dotnet run" C-m
        tmux split-window -v -t docn:servers
        tmux send-keys -t docn:servers "sleep 5 && cd '$SCRIPT_DIR/DocN.Client' && dotnet run" C-m
        tmux attach-session -t docn:servers
    else
        echo "Please open two terminals and run:"
        echo "Terminal 1: cd '$SCRIPT_DIR/DocN.Server' && dotnet run"
        echo "Terminal 2: cd '$SCRIPT_DIR/DocN.Client' && dotnet run"
    fi
else
    echo "Please open two terminals and run:"
    echo "Terminal 1: cd '$SCRIPT_DIR/DocN.Server' && dotnet run"
    echo "Terminal 2: cd '$SCRIPT_DIR/DocN.Client' && dotnet run"
fi
