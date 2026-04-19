#!/bin/bash
# Startup script for Render deployment
# This script prepares the environment before starting the server

echo "🚀 Starting Detective Game Backend initialization..."

# Create necessary directories
mkdir -p /tmp/game_db
mkdir -p /tmp/police_guidebook_db

# Check if database needs to be initialized
if [ ! -d "/tmp/game_db/chroma" ]; then
    echo "📦 Initializing ChromaDB database..."
    python create_knowledge_base.py

    # Move database to persistent storage
    if [ -d "./game_db" ]; then
        cp -r ./game_db/* /tmp/game_db/
        echo "✅ Database copied to /tmp/game_db/"
    fi
fi

# Update server.py to use /tmp for persistent storage
# This is handled by environment variable or by modifying DB_PATH
export DB_PATH="/tmp/game_db"

echo "✅ Initialization complete. Starting server..."
exec "$@"
