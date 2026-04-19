# Quick Start - Render Deployment

## 1️⃣ Prepare Your Code

```bash
# In Backend/rag/
git add .
git commit -m "Add Render deployment config"
git push
```

## 2️⃣ Deploy on Render

**URL**: [dashboard.render.com](https://dashboard.render.com)

1. Click **"New +"** → **"Web Service"**
2. Connect GitHub → Select `SeniorProject` repo
3. Configure:
   - **Name**: `detective-game-backend`
   - **Runtime**: `Python`
   - **Build Command**: `pip install -r requirements.txt && python init_on_start.py`
   - **Start Command**: `uvicorn server:app --host 0.0.0.0 --port $PORT`

## 3️⃣ Set Environment Variable

In Render Dashboard → **Environment**:

```
GROQ_API_KEY = your_actual_groq_api_key_here
```

## 4️⃣ Deploy & Get URL

After deploy (~3 min), you'll get:
```
https://detective-game-backend.onrender.com
```

## 5️⃣ Update Unity

In **GameManagerSimple** → **LLMClientSimple**:
- Change **Base URL** to your Render URL

## ✅ Done!

Test the game with your live backend!

---

## Files Created for Render Deployment

| File | Purpose |
|------|---------|
| `requirements.txt` | Python dependencies |
| `render.yaml` | Render configuration (optional) |
| `init_on_start.py` | Database initialization script |
| `.renderignore` | Files to exclude from deploy |
| `RENDER_DEPLOYMENT_GUIDE.md` | Full deployment guide |

---

## Need the full guide?

See [RENDER_DEPLOYMENT_GUIDE.md](RENDER_DEPLOYMENT_GUIDE.md) for:
- Detailed step-by-step instructions
- Troubleshooting tips
- CORS setup
- Maintenance guide
