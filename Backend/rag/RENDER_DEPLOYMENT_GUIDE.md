# Render Deployment Guide - Detective Game Backend

## Prerequisites

1. **Render Account**: Sign up at [render.com](https://render.com)
2. **GitHub Repository**: Your code must be on GitHub
3. **Groq API Key**: Get your free API key from [groq.com](https://groq.com)

---

## Step 1: Prepare Your Codebase

### 1.1 Update `server.py` for Render compatibility

Replace the CONFIG section (lines 14-26) in `server.py` with Render-compatible code:

```python
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

# Load case truth from file
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
```

### 1.2 Create initialization script

Create `init_on_start.py` in `Backend/rag/`:

```python
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
```

### 1.3 Update `requirements.txt`

Make sure your `requirements.txt` includes:

```
fastapi==0.115.0
uvicorn[standard]==0.32.0
pydantic==2.9.2
chromadb==0.5.23
groq==0.11.0
```

### 1.4 Commit and push to GitHub

```bash
git add .
git commit -m "Add Render deployment configuration"
git push origin main
```

---

## Step 2: Deploy on Render

### 2.1 Create a New Web Service

1. Go to [dashboard.render.com](https://dashboard.render.com)
2. Click **"New +"** → **"Web Service"**
3. Connect your GitHub account (if not already connected)
4. Select your `SeniorProject` repository
5. Configure the service:

**Basic Settings:**
- **Name**: `detective-game-backend`
- **Region**: Singapore (or closest to your players)
- **Branch**: `main`

**Build & Runtime:**
- **Runtime**: `Python`
- **Build Command**: `pip install -r requirements.txt && python init_on_start.py`
- **Start Command**: `uvicorn server:app --host 0.0.0.0 --port $PORT`

### 2.2 Configure Environment Variables

Click **"Advanced"** → **"Add Environment Variable"**:

| Key | Value | Required |
|-----|-------|----------|
| `GROQ_API_KEY` | Your Groq API key | ✅ Yes |
| `DB_PATH` | `/tmp/game_db` | No (defaults to ./game_db) |
| `MODEL_NAME` | `llama-3.1-8b-instant` | No |
| `PYTHON_VERSION` | `3.9.0` | No |

**Important**: Set `GROQ_API_KEY` to your actual Groq API key!

### 2.3 Deploy

Click **"Create Web Service"** and wait for deployment:
- Build: ~2-3 minutes
- Database initialization: ~1 minute
- Total: ~3-4 minutes

You'll see logs showing:
```
📦 Initializing ChromaDB database...
✅ Database initialized successfully
INFO:     Started server process
INFO:     Waiting for application startup.
INFO:     Application startup complete.
INFO:     Uvicorn running on port 10000
```

---

## Step 3: Get Your API URL

Once deployed, Render will give you a URL like:
```
https://detective-game-backend.onrender.com
```

Your API endpoints will be:
- `POST https://detective-game-backend.onrender.com/start-game`
- `POST https://detective-game-backend.onrender.com/chat`
- `POST https://detective-game-backend.onrender.com/collect-evidence`
- `POST https://detective-game-backend.onrender.com/use-evidence`
- `POST https://detective-game-backend.onrender.com/evaluate-case`
- `GET  https://detective-game-backend.onrender.com/final-score`

---

## Step 4: Update Unity to Use Render URL

In Unity Editor:

1. Open **GameManagerSimple** GameObject
2. In **LLMClientSimple** component
3. Change **Base URL** from `http://127.0.0.1:8000` to:
   ```
   https://detective-game-backend.onrender.com
   ```

**Note**: The Unity WebGL build may have CORS issues. If so, you'll need to add CORS middleware to your FastAPI server.

---

## Step 5: Add CORS Support (if needed)

If you're building for WebGL or accessing from a browser, add CORS support to `server.py`:

```python
from fastapi.middleware.cors import CORSMiddleware

# Add after app = FastAPI(...)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify your Unity game URL
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
```

---

## Important Limitations (Free Tier)

⚠️ **Known Issues with Render Free Tier:**

1. **Cold Starts**: Your server will sleep after 15 minutes of inactivity
   - First request after sleep: ~30-60 seconds startup time
   - Solution: Ping your server every 5 minutes to keep it awake

2. **Database Persistence**: Files are wiped on each deploy
   - Your `game_state.json` and `/tmp` files persist between restarts
   - But `game_db/` will be rebuilt on each deploy
   - Solution: Use the initialization script to rebuild automatically

3. **Request Timeout**: Free tier has 90-second timeout
   - Your RAG queries should be fast enough
   - LLM calls via Groq are typically <5 seconds

---

## Maintenance & Monitoring

### View Logs
Go to Render Dashboard → Your Service → **Logs**

### Check Health
Visit: `https://detective-game-backend.onrender.com/docs` (FastAPI auto-generated docs)

### Automatic Restarts
Render automatically restarts your service if it crashes. Check logs for errors.

---

## Troubleshooting

### Issue: "Database not loaded" error
**Solution**: The initialization script didn't run. Check build logs and ensure `init_on_start.py` is in your repository.

### Issue: "GROQ_API_KEY not set"
**Solution**: Add the environment variable in Render Dashboard → Your Service → Environment

### Issue: Very slow first request
**Solution**: This is normal (cold start). Subsequent requests will be fast.

### Issue: 504 Gateway Timeout
**Solution**: Your request is taking too long. Consider:
- Using a faster Groq model
- Reducing RAG retrieval count
- Optimizing your prompts

### Issue: CORS errors in Unity
**Solution**: Add the CORS middleware shown in Step 5.

---

## Next Steps

1. ✅ Deploy backend to Render
2. ✅ Update Unity with Render URL
3. ✅ Test the game end-to-end
4. ✅ Set up monitoring/alerts (optional)
5. ✅ Consider upgrading to paid tier for better performance ($7/month)

---

## Cost Summary

**Free Tier (Current)**:
- ✅ 512 MB RAM
- ✅ 0.1 CPU
- ✅ 750 hours/month
- ❌ Cold starts (30-60s)
- ❌ Ephemeral file system

**Paid Starter ($7/month)**:
- ✅ 512 MB RAM
- ✅ 0.5 CPU (5x faster)
- ✅ No cold starts
- ✅ Persistent disk available
- ✅ Better for production

---

Need help? Check:
- [Render Python Documentation](https://render.com/docs/deploy-python)
- [FastAPI Deployment Guide](https://fastapi.tiangolo.com/deployment/)
- [Groq API Documentation](https://console.groq.com/docs)
