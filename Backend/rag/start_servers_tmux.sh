#!/bin/bash

# Start both servers using tmux
# More reliable than osascript, works on macOS and Linux

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Check if tmux is installed
if ! command -v tmux &> /dev/null; then
    echo "❌ tmux is not installed!"
    echo "Install it with: brew install tmux (macOS) or apt install tmux (Linux)"
    echo ""
    echo "Alternatively, run the servers manually in two separate terminals:"
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

# Session name
SESSION="detective-servers"

# Check if session already exists
if tmux has-session -t $SESSION 2>/dev/null; then
    echo "⚠️  Session '$SESSION' already exists!"
    echo "Attach to it with: tmux attach -t $SESSION"
    echo "Or kill it first with: tmux kill-session -t $SESSION"
    exit 1
fi

echo "🚀 Starting both servers in tmux session: $SESSION"
echo ""

# Create new session and start English server
tmux new-session -d -s $SESSION -n "English-Server"

# Set API keys (you'll be prompted if not set)
if [ -z "$GROQ_API_KEY" ]; then
    read -p "Enter your Groq API key: " GROQ_API_KEY
fi

if [ -z "$TYPHOON_API_KEY" ]; then
    read -p "Enter your Typhoon API key: " TYPHOON_API_KEY
fi

# Start English server in first window
tmux send-keys -t $SESSION:0 "cd '$SCRIPT_DIR'" C-m
tmux send-keys -t $SESSION:0 "export GROQ_API_KEY='$GROQ_API_KEY'" C-m
tmux send-keys -t $SESSION:0 "echo '🔵 Starting English Server (Case 1) on port 8000...'" C-m
tmux send-keys -t $SESSION:0 "uvicorn server:app --reload --host 127.0.0.1 --port 8000" C-m

# Create second window for Thai server
tmux new-window -t $SESSION:1 -n "Thai-Server"
tmux send-keys -t $SESSION:1 "cd '$SCRIPT_DIR'" C-m
tmux send-keys -t $SESSION:1 "export TYPHOON_API_KEY='$TYPHOON_API_KEY'" C-m
tmux send-keys -t $SESSION:1 "echo '🔴 Starting Thai Server (Case 2) on port 8001...'" C-m
tmux send-keys -t $SESSION:1 "uvicorn server_thai:app --reload --host 127.0.0.1 --port 8001" C-m

# Attach to the session
echo "✅ Both servers are starting in tmux session '$SESSION'!"
echo ""
echo "📋 tmux commands:"
echo "  - Switch between windows: Ctrl+B then 0 (English) or 1 (Thai)"
echo "  - Detach from session: Ctrl+B then D"
echo "  - Reattach to session: tmux attach -t $SESSION"
echo "  - Kill session: tmux kill-session -t $SESSION"
echo "  - List all windows: Ctrl+B then W"
echo ""
echo "Attaching to session..."
tmux attach-session -t $SESSION
