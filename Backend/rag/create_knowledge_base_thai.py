import chromadb
import os

# =========================
# CONFIG (Thai Version)
# =========================
DATA_FILE = "case2_data_Thai.txt"
DB_PATH = "./game_db_thai"
COLLECTION_NAME = "murder_case_thai"

# =========================
# CREATE VECTOR DATABASE
# =========================
def create_database():

    # Check if data file exists
    if not os.path.exists(DATA_FILE):
        print(f"❌ Error: ไม่พบไฟล์ {DATA_FILE}")
        return

    print(f"📖 Reading {DATA_FILE}...")

    documents = []
    metadatas = []
    ids = []

    current_owner = "ALL"

    # -------------------------
    # Read and parse case data
    # -------------------------
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        for i, line in enumerate(f):
            line = line.strip()

            if not line:
                continue

            # Switch NPC owner e.g., [PORNTIP]
            if line.startswith("[") and line.endswith("]"):
                current_owner = line[1:-1].upper()
                print(f"   👉 Switch to owner: {current_owner}")
                continue

            documents.append(line)
            metadatas.append({"owner": current_owner})
            ids.append(f"fact_{i}")

    print(f"📦 Total facts loaded: {len(documents)}")

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

    # Use default embedding (no sentence-transformers)
    collection = client.create_collection(name=COLLECTION_NAME)

    # Add data to vector DB
    collection.add(
        documents=documents,
        metadatas=metadatas,
        ids=ids
    )

    print(f"✅ Saved {len(documents)} facts into database.")
    print("🎉 Vector database is ready!")

# =========================
# MAIN
# =========================
if __name__ == "__main__":
    create_database()
