#!/bin/bash

# Start both servers for the detective game
# This script opens two terminal windows and runs both servers

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Check if API keys are set
if [ -z "$GROQ_API_KEY" ]; then
    echo "⚠️  GROQ_API_KEY not set!"
    echo "Please set it first: export GROQ_API_KEY='your-key'"
    read -p "Enter your Groq API key: " GROQ_API_KEY
    export GROQ_API_KEY
fi

if [ -z "$TYPHOON_API_KEY" ]; then
    echo "⚠️  TYPHOON_API_KEY not set!"
    echo "Please set it first: export TYPHOON_API_KEY='your-key'"
    read -p "Enter your Typhoon API key: " TYPHOON_API_KEY
    export TYPHOON_API_KEY
fi

echo "🚀 Starting both servers..."
echo "📦 English Server (Case 1) on port 8000"
echo "📦 Thai Server (Case 2) on port 8001"
echo ""
echo "Press Ctrl+C in each terminal to stop the servers"
echo ""

# macOS: Use osascript to open new Terminal windows
if [[ "$OSTYPE" == "darwin"* ]]; then
    # Terminal 1 - English Server
    osascript <<EOF
tell application "Terminal"
    activate
    do script "cd '$SCRIPT_DIR' && export GROQ_API_KEY='$GROQ_API_KEY' && echo '🔵 Starting English Server (Case 1) on port 8000...' && uvicorn server:app --reload --host 127.0.0.1 --port 8000"
end tell
EOF

    # Terminal 2 - Thai Server
    osascript <<EOF
tell application "Terminal"
    activate
    do script "cd '$SCRIPT_DIR' && export TYPHOON_API_KEY='$TYPHOON_API_KEY' && echo '🔴 Starting Thai Server (Case 2) on port 8001...' && uvicorn server_thai:app --reload --host 127.0.0.1 --port 8001"
end tell
EOF

else
    # Linux: Use gnome-terminal or xterm
    if command -v gnome-terminal &> /dev/null; then
        gnome-terminal -- bash -c "cd '$SCRIPT_DIR' && export GROQ_API_KEY='$GROQ_API_KEY' && echo '🔵 Starting English Server (Case 1) on port 8000...' && uvicorn server:app --reload --host 127.0.0.1 --port 8000; exec bash"
        gnome-terminal -- bash -c "cd '$SCRIPT_DIR' && export TYPHOON_API_KEY='$TYPHOON_API_KEY' && echo '🔴 Starting Thai Server (Case 2) on port 8001...' && uvicorn server_thai:app --reload --host 127.0.0.1 --port 8001; exec bash"
    elif command -v xterm &> /dev/null; then
        xterm -e "cd '$SCRIPT_DIR' && export GROQ_API_KEY='$GROQ_API_KEY' && echo '🔵 Starting English Server (Case 1) on port 8000...' && uvicorn server:app --reload --host 127.0.0.1 --port 8000" &
        xterm -e "cd '$SCRIPT_DIR' && export TYPHOON_API_KEY='$TYPHOON_API_KEY' && echo '🔴 Starting Thai Server (Case 2) on port 8001...' && uvicorn server_thai:app --reload --host 127.0.0.1 --port 8001" &
    else
        echo "❌ Could not detect terminal emulator. Please run the servers manually in separate terminals."
        echo ""
        echo "Terminal 1 (English):"
        echo "  cd Backend/rag"
        echo "  export GROQ_API_KEY='your-key'"
        echo "  uvicorn server:app --reload --host 127.0.0.1 --port 8000"
        echo ""
        echo "Terminal 2 (Thai):"
        echo "  cd Backend/rag"
        echo "  export TYPHOON_API_KEY='your-key'"
        echo "  uvicorn server_thai:app --reload --host 127.0.0.1 --port 8001"
        exit 1
    fi
fi

echo "✅ Both servers should now be starting in separate terminal windows!"
