import chromadb
import os

# =========================
# CONFIG
# =========================
DATA_FILE = "police_interrogation_rules.txt"
DB_PATH = "./game_db"
COLLECTION_NAME = "police_rules"

# =========================
# CREATE VECTOR DATABASE
# =========================
def create_police_rules_database():

    if not os.path.exists(DATA_FILE):
        print(f"❌ Error: ไม่พบไฟล์ {DATA_FILE}")
        return

    print(f"📖 Reading {DATA_FILE}...")

    documents = []
    metadatas = []
    ids = []

    current_tag = "ALL"

    # -------------------------
    # Read and parse rules
    # -------------------------
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        for i, line in enumerate(f):
            line = line.strip()

            if not line:
                continue

            # Detect tag like [POLITENESS]
            if line.startswith("[") and line.endswith("]"):
                current_tag = line[1:-1].upper()
                print(f"   👉 Switch to tag: {current_tag}")
                continue

            documents.append(line)
            metadatas.append({"tag": current_tag})
            ids.append(f"rule_{i}")

    print(f"📦 Total rules loaded: {len(documents)}")

    # -------------------------
    # Create ChromaDB
    # -------------------------
    client = chromadb.PersistentClient(path=DB_PATH)

    try:
        client.delete_collection(name=COLLECTION_NAME)
        print("🗑️  Old police_rules collection deleted")
    except:
        pass

    # ❗ ใช้ default embedding เหมือน murder_case
    collection = client.create_collection(name=COLLECTION_NAME)

    collection.add(
        documents=documents,
        metadatas=metadatas,
        ids=ids
    )

    print(f"✅ Saved {len(documents)} police rules into database.")
    print("🎉 police_rules vector database is ready!")

# =========================
# MAIN
# =========================
if __name__ == "__main__":
    create_police_rules_database()