import chromadb
import os

# =========================
# CONFIG
# =========================
DATA_FILE = "case_data.txt"
DB_PATH = "./game_db"
COLLECTION_NAME = "murder_case"

# =========================
# CREATE VECTOR DATABASE
# =========================
def create_database():

    # ตรวจว่าไฟล์ข้อมูลมีอยู่จริงไหม
    if not os.path.exists(DATA_FILE):
        print(f"❌ Error: ไม่พบไฟล์ {DATA_FILE}")
        return

    print(f"📖 Reading {DATA_FILE}...")

    documents = []
    metadatas = []
    ids = []

    current_owner = "ALL"
    current_chunk = []
    current_category = None

    # -------------------------
    # อ่านและ parse case data
    # -------------------------
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        for i, line in enumerate(f):
            line = line.strip()

            if not line:
                # Save current chunk when hitting empty line
                if current_chunk:
                    chunk_text = "\n".join(current_chunk).strip()
                    if chunk_text:
                        documents.append(chunk_text)
                        metadatas.append({"owner": current_owner, "category": current_category or "general"})
                        ids.append(f"fact_{len(documents)}")
                    current_chunk = []
                    current_category = None
                continue

            # เปลี่ยน NPC owner เช่น [BRIAN]
            if line.startswith("[") and line.endswith("]"):
                # Save previous chunk before switching
                if current_chunk:
                    chunk_text = "\n".join(current_chunk).strip()
                    if chunk_text:
                        documents.append(chunk_text)
                        metadatas.append({"owner": current_owner, "category": current_category or "general"})
                        ids.append(f"fact_{len(documents)}")
                    current_chunk = []

                current_owner = line[1:-1].upper()
                print(f"   👉 Switch to owner: {current_owner}")
                continue

            # Detect category changes for semantic chunking
            if line.startswith("-") or line.startswith("Timeline") or line.startswith("Knowledge"):
                # Start of a new logical chunk
                if current_chunk and current_category:
                    # Save previous chunk
                    chunk_text = "\n".join(current_chunk).strip()
                    if chunk_text:
                        documents.append(chunk_text)
                        metadatas.append({"owner": current_owner, "category": current_category})
                        ids.append(f"fact_{len(documents)}")
                    current_chunk = []

                # Determine category
                if "Timeline" in line or "PM" in line or "PM" in line:
                    current_category = "timeline"
                elif "Knowledge" in line:
                    current_category = "knowledge"
                else:
                    current_category = "general"

            # Add line to current chunk
            current_chunk.append(line)

    # Save final chunk
    if current_chunk:
        chunk_text = "\n".join(current_chunk).strip()
        if chunk_text:
            documents.append(chunk_text)
            metadatas.append({"owner": current_owner, "category": current_category or "general"})
            ids.append(f"fact_{len(documents)}")

    print(f"📦 Total semantic chunks loaded: {len(documents)}")

    # -------------------------
    # Create ChromaDB
    # -------------------------
    client = chromadb.PersistentClient(path=DB_PATH)

    # ลบ collection เดิม (ถ้ามี)
    try:
        client.delete_collection(name=COLLECTION_NAME)
        print("🗑️  Old collection deleted")
    except:
        pass

    # ❗ ใช้ default embedding (ไม่ใช้ sentence-transformers)
    collection = client.create_collection(name=COLLECTION_NAME)

    # เพิ่มข้อมูลเข้า vector DB
    collection.add(
        documents=documents,
        metadatas=metadatas,
        ids=ids
    )

    print(f"✅ Saved {len(documents)} semantic chunks into database.")
    print("🎉 Vector database is ready!")

    # Print sample chunks for verification
    print("\n📋 Sample chunks:")
    for i in range(min(3, len(documents))):
        print(f"\n--- Chunk {i+1} ({metadatas[i]['owner']}, {metadatas[i].get('category', 'general')}) ---")
        print(documents[i][:200] + "..." if len(documents[i]) > 200 else documents[i])

# =========================
# MAIN
# =========================
if __name__ == "__main__":
    create_database()
