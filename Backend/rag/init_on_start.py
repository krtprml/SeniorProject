#!/usr/bin/env python3
"""
Initialize ChromaDB database on Render startup
This script runs before the main server starts
"""
import os
import subprocess
import sys

def initialize_database():
    """Initialize the ChromaDB database if it doesn't exist"""
    db_path = os.getenv("DB_PATH", "./game_db")

    # Create directory if it doesn't exist
    os.makedirs(db_path, exist_ok=True)

    # Check if database already exists
    if os.path.exists(os.path.join(db_path, "chroma")):
        print("✅ Database already exists")
        return

    print("📦 Initializing ChromaDB database...")

    # Run the knowledge base creation script
    try:
        subprocess.run([sys.executable, "create_knowledge_base.py"], check=True)
        print("✅ Database initialized successfully")
    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to initialize database: {e}")
        sys.exit(1)

if __name__ == "__main__":
    initialize_database()
