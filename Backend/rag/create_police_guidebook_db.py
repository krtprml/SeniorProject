#!/usr/bin/env python3
"""
Create ChromaDB vector database from Thai police guidebook
For RAG-based question evaluation reasoning
"""

import chromadb
import os
import re

# =========================
# CONFIG
# =========================
DATA_FILE = "police_guidebook.txt"
DB_PATH = "./police_guidebook_db"
COLLECTION_NAME = "police_guidebook"

# =========================
# CREATE VECTOR DATABASE
# =========================
def create_police_guidebook_db():
    """Create vector database from Thai police guidebook for question evaluation"""

    # Check if data file exists
    if not os.path.exists(DATA_FILE):
        print(f"❌ Error: ไม่พบไฟล์ {DATA_FILE}")
        return

    print(f"📖 Reading {DATA_FILE}...")

    documents = []
    metadatas = []
    ids = []

    current_section = "general"
    current_chunk = []
    chunk_id = 0

    # -------------------------
    # Read and parse guidebook
    # -------------------------
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        for i, line in enumerate(f):
            line = line.strip()

            # Skip empty lines
            if not line:
                # Save current chunk when hitting empty line
                if current_chunk:
                    chunk_text = "\n".join(current_chunk).strip()
                    if len(chunk_text) > 50:  # Only save meaningful chunks
                        documents.append(chunk_text)
                        metadatas.append({"section": current_section, "line_start": i - len(current_chunk)})
                        ids.append(f"guide_{chunk_id}")
                        chunk_id += 1
                    current_chunk = []
                continue

            # Detect section headers like [CH1], [CH2]
            if line.startswith("[CH") or line.startswith("[บท"):
                # Save previous chunk
                if current_chunk:
                    chunk_text = "\n".join(current_chunk).strip()
                    if len(chunk_text) > 50:
                        documents.append(chunk_text)
                        metadatas.append({"section": current_section, "line_start": i - len(current_chunk)})
                        ids.append(f"guide_{chunk_id}")
                        chunk_id += 1
                    current_chunk = []

                current_section = line
                print(f"   👉 Section: {current_section}")
                continue

            # Detect subsection headers (numbering like 1., 1.1, 1.1.1)
            if re.match(r'^\d+\.\d*\s+', line):
                # Save previous chunk
                if current_chunk:
                    chunk_text = "\n".join(current_chunk).strip()
                    if len(chunk_text) > 50:
                        documents.append(chunk_text)
                        metadatas.append({"section": current_section, "line_start": i - len(current_chunk)})
                        ids.append(f"guide_{chunk_id}")
                        chunk_id += 1
                    current_chunk = []

                # Start new chunk with subsection
                current_chunk.append(line)
                continue

            # Detect bullet points or list items
            if line.startswith("-") or line.startswith("(") or line.startswith("("):
                # Save chunk if it's getting long
                if len(current_chunk) > 10:
                    chunk_text = "\n".join(current_chunk).strip()
                    if len(chunk_text) > 50:
                        documents.append(chunk_text)
                        metadatas.append({"section": current_section, "line_start": i - len(current_chunk)})
                        ids.append(f"guide_{chunk_id}")
                        chunk_id += 1
                    current_chunk = []

            # Add line to current chunk
            current_chunk.append(line)

    # Save final chunk
    if current_chunk:
        chunk_text = "\n".join(current_chunk).strip()
        if len(chunk_text) > 50:
            documents.append(chunk_text)
            metadatas.append({"section": current_section, "line_start": i - len(current_chunk)})
            ids.append(f"guide_{chunk_id}")

    print(f"📦 Total chunks loaded: {len(documents)}")

    # -------------------------
    # Create ChromaDB
    # -------------------------
    client = chromadb.PersistentClient(path=DB_PATH)

    # Delete old collection (if exists)
    try:
        client.delete_collection(name=COLLECTION_NAME)
        print("🗑️  Old collection deleted")
    except:
        pass

    # Use default embedding
    collection = client.create_collection(name=COLLECTION_NAME)

    # Add documents to vector DB
    collection.add(
        documents=documents,
        metadatas=metadatas,
        ids=ids
    )

    print(f"✅ Saved {len(documents)} chunks into police guidebook database.")
    print("🎉 Police guidebook vector database is ready!")

    # Print sample chunks for verification
    print("\n📋 Sample chunks:")
    for i in range(min(3, len(documents))):
        print(f"\n--- Chunk {i+1} ({metadatas[i]['section']}) ---")
        preview = documents[i][:200] + "..." if len(documents[i]) > 200 else documents[i]
        print(preview)

# =========================
# MAIN
# =========================
if __name__ == "__main__":
    create_police_guidebook_db()
    print("\n💡 Usage in server_thai.py:")
    print("   Add: POLICE_GUIDEBOOK_DB_PATH = './police_guidebook_db'")
    print("   Load: police_guidebook_collection = chroma_client.get_collection('police_guidebook')")
    print("   Query: search_police_guidebook(question, evaluation_results)")
