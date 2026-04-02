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

    # -------------------------
    # อ่านและ parse case data
    # -------------------------
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        for i, line in enumerate(f):
            line = line.strip()

            if not line:
                continue

            # เปลี่ยน NPC owner เช่น [BRIAN]
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

    print(f"✅ Saved {len(documents)} facts into database.")
    print("🎉 Vector database is ready!")

# =========================
# MAIN
# =========================
if __name__ == "__main__":
    create_database()