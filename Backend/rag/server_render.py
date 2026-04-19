# Render-specific configuration changes for server.py
# Replace lines 14-26 in server.py with this code:

# ==============================
# CONFIG (Render-compatible)
# ==============================
# For Render deployment, use /tmp for persistent storage
DB_PATH = os.getenv("DB_PATH", "./game_db")
MURDER_COLLECTION = "murder_case"

# For Render, store game state in /tmp to persist across restarts
GAME_STATE_FILE = os.path.join(os.getenv("TMPDIR", "/tmp"), "game_state.json")

GROQ_API_KEY = os.getenv("GROQ_API_KEY")
if not GROQ_API_KEY:
    raise ValueError("GROQ_API_KEY environment variable is required")

MODEL_NAME = os.getenv("MODEL_NAME", "llama-3.1-8b-instant")
MAX_MEMORY_TURNS = int(os.getenv("MAX_MEMORY_TURNS", "4"))

# Load case truth from file (handle both relative and absolute paths)
case_truth_path = os.getenv("CASE_TRUTH_PATH", "case_truth.txt")
try:
    with open(case_truth_path, "r", encoding="utf-8") as f:
        CASE_CONTEXT = f.read().strip()
except FileNotFoundError:
    raise FileNotFoundError(f"Case truth file not found: {case_truth_path}")

# Load evidence data
evidence_data_path = os.getenv("EVIDENCE_DATA_PATH", "evidence_data.json")
try:
    with open(evidence_data_path, "r", encoding="utf-8") as f:
        EVIDENCE_DATA = json.load(f)
except FileNotFoundError:
    raise FileNotFoundError(f"Evidence data file not found: {evidence_data_path}")

# Port configuration for Render
PORT = int(os.getenv("PORT", "8000"))
