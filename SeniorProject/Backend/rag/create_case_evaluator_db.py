import chromadb

DATA_FILE = "case_truth.txt"
DB_PATH = "./game_db"
COLLECTION = "case_evaluator"

client = chromadb.PersistentClient(path=DB_PATH)

try:
    client.delete_collection(COLLECTION)
except:
    pass

collection = client.create_collection(COLLECTION)

docs = []
ids = []

with open(DATA_FILE) as f:
    for i, line in enumerate(f):
        if line.strip():
            docs.append(line.strip())
            ids.append(f"t{i}")

collection.add(documents=docs, ids=ids)
print("CASE evaluator DB ready")