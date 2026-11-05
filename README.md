# 🕵️ Murder Mystery AI Game — Senior Project

> เกมสืบสวนฆาตกรรมที่ NPC "ไม่ได้แค่พูด" — แต่โกหก จำบทสนทนา และยอมรับความจริงเมื่อถูกเผชิญหน้าด้วยหลักฐาน

เกมสืบสวนมุมมองบุคคลที่หนึ่ง พัฒนาด้วย **Unity + FastAPI + LLM (RAG)** โดยมี 2 คดีให้เลือกเล่น:

- **Case 1 — คดีฆาตกรรม Victor** (English)
- **Case 2 — คดี "พินัยกรรมในห้องปิดตาย"** (ภาษาไทย)

ทุกคำพูดของ NPC ถูกสร้างสดโดย AI ผ่าน API จริง ดังนั้นก่อนเข้าเกม **ผู้เล่นต้องเตรียม API Key และเลือก Model เอง** (ขั้นตอนที่ 3–4)

---

## ⚠️ อ่านก่อนเริ่ม — ทำไมต้องใส่ API Key เอง

Model ที่ถูก set ค่าไว้ในโค้ดเดิม **หมดอายุ / ถูกย้ายไป Enterprise tier แล้ว**:

| ไฟล์ | Model เดิมในโค้ด | สถานะปัจจุบัน |
|------|------------------|----------------|
| `server.py` | `llama-3.1-8b-instant` (Groq) | ย้ายไป Enterprise tier — key ฟรีเรียกไม่ได้ |
| `server_thai.py` | `typhoon-v2.5-30b-a3b-instruct` | ยังใช้ได้ (ต้องมี Typhoon key) |
| `server_thai_gemini.py` | `gemini-3.1-flash-lite-preview` | Preview model หมดอายุแล้ว |

ดังนั้น **ทุกคนต้องสมัคร API key ของตัวเอง และแก้ `MODEL_NAME` เป็น model ที่ยังให้บริการอยู่** ก่อนรันเกม — ทุกตัวที่แนะนำด้านล่างมี tier ฟรีให้ใช้

---

## 📋 สรุปการตั้งค่าทั้งหมด

| คดี | ภาษา | ไฟล์ Server | ผู้ให้บริการ AI | Environment Variable | Model ที่แนะนำ | Port |
|-----|------|--------------|------------------|----------------------|-----------------|------|
| Case 1 | 🇬🇧 EN | `server.py` | Groq | `GROQ_API_KEY` | `openai/gpt-oss-20b` | 8000 |
| Case 2 | 🇹🇭 TH | `server_thai.py` | Typhoon (SCB 10X) | `TYPHOON_API_KEY` | `typhoon-v2.5-30b-a3b-instruct` | 8001 |
| Case 2 (ทางเลือก) | 🇹🇭 TH | `server_thai_gemini.py` | Google Gemini | `GEMINI_API_KEY` | `gemini-2.5-flash-lite` | 8001 |

> เลือกใช้ **อย่างใดอย่างหนึ่งต่อคดี** — เล่น Case 2 ได้ทั้ง Typhoon หรือ Gemini (รันแทนกันที่ port 8001)

---

## 🧰 สิ่งที่ต้องมี

- **Unity 6000.1.13f1 (Unity 6)** — ติดตั้งผ่าน [Unity Hub](https://unity.com/download)
- **Python 3.10+**
- **บัญชีผู้ให้บริการ AI** (ตามคดีที่จะเล่น — ดูขั้นตอนที่ 3)
- อินเทอร์เน็ต (การสร้าง Vector Database ครั้งแรกจะดาวน์โหลด embedding model อัตโนมัติ)

---

## ขั้นตอนที่ 1 — ติดตั้งโปรเจกต์

```bash
git clone https://github.com/krtprml/SeniorProject.git
cd SeniorProject/Backend/rag

# สร้าง virtual environment (แนะนำ)
python3 -m venv venv
source venv/bin/activate        # macOS / Linux
# venv\Scripts\activate         # Windows

# ติดตั้ง dependencies
pip install -r requirements.txt

# เพิ่มเติมสำหรับ server ภาษาไทย (ยังไม่ได้อยู่ใน requirements.txt)
pip install openai google-genai
```

> `openai` ใช้กับ `server_thai.py` (Typhoon) และ `google-genai` ใช้กับ `server_thai_gemini.py` — ถ้าเล่นแค่ Case 1 ไม่ต้องติดตั้งก็ได้ แต่ติดตั้งไว้เลยสะดวกกว่า

---

## ขั้นตอนที่ 2 — สร้าง Vector Database (จำเป็นก่อนรันครั้งแรก)

โฟลเดอร์ `game_db/` และ `game_db_thai/` **ไม่ได้ถูกเก็บใน git** (ถูก ignore ไว้) ต้องสร้างเองก่อน ไม่งั้น NPC จะตอบไม่ได้:

```bash
# อยู่ที่ Backend/rag/
python create_knowledge_base.py        # สำหรับ Case 1 (EN) → สร้าง game_db/
python create_knowledge_base_thai.py   # สำหรับ Case 2 (TH) → สร้าง game_db_thai/
```

รันแค่คดีที่จะเล่น หรือรันทั้งสองอันเลยก็ได้ (รันครั้งเดียวพอ ไม่ต้องรันซ้ำทุกครั้งที่เปิดเกม)

---

## ขั้นตอนที่ 3 — สมัครขอ API Key

| ผู้ให้บริการ | สมัครที่ | ใช้กับ |
|--------------|----------|--------|
| 🟣 **Groq** | https://console.groq.com/keys | Case 1 (EN) |
| 🌊 **Typhoon (SCB 10X)** | https://opentyphoon.ai | Case 2 (TH — Typhoon) |
| 🔵 **Google AI Studio** | https://aistudio.google.com/apikey | Case 2 (TH — Gemini) |

สมัครฟรี สร้าง key แล้วคัดลอกเก็บไว้ (key จะแสดงครั้งเดียว)

---

## ขั้นตอนที่ 4 — ใส่ API Key + เลือก Model ⭐ (สำคัญที่สุด)

### 4.1 ใส่ API Key

**วิธีที่ 1: Environment Variable (แนะนำ)** — export ใน terminal **เดียวกับที่จะรัน server**:

```bash
# macOS / Linux — เลือกตาม server ที่จะใช้
export GROQ_API_KEY="gsk_xxxxxxxx"       # Case 1 (EN)
export TYPHOON_API_KEY="sk-xxxxxx"       # Case 2 (TH - Typhoon)
export GEMINI_API_KEY="AIzaxxxxxx"       # Case 2 (TH - Gemini)

# Windows PowerShell
$env:GROQ_API_KEY="gsk_xxxxxxxx"
```

**วิธีที่ 2: แก้ในไฟล์โค้ด** — หาบรรทัด fallback แล้วใส่ key แทนคำว่า `PUT_YOUR_..._KEY_HERE`:

| ไฟล์ | บรรทัดที่แก้ | แก้จาก → เป็น |
|------|--------------|----------------|
| `server.py` | บรรทัด 23 | `"PUT_YOUR_GROQ_KEY_HERE"` → `"gsk_xxxxxxxx"` |
| `server_thai.py` | บรรทัด 23 | `"PUT_YOUR_TYPHOON_KEY_HERE"` → key ของคุณ |
| `server_thai_gemini.py` | บรรทัด 24 | `"PUT_YOUR_GEMINI_KEY_HERE"` → key ของคุณ |

> ⚠️ **ห้าม commit key ขึ้น git เด็ดขาด** — แนะนำวิธีที่ 1 (env var) เพื่อกัน key หลุดขึ้น repo
>
> ℹ️ การสร้างไฟล์ `.env` **ไม่ทำงาน** เพราะโค้ดไม่ได้ใช้ python-dotenv — ต้อง export ผ่าน terminal เท่านั้น

### 4.2 เลือก Model

แก้บรรทัด `MODEL_NAME = "..."` ในไฟล์ server ที่จะใช้:

| ไฟล์ | บรรทัด | ค่าเดิม | แก้เป็น (แนะนำ) |
|------|--------|---------|------------------|
| `server.py` | 24 | `llama-3.1-8b-instant` ❌ | `openai/gpt-oss-20b` ✅ |
| `server_thai.py` | 25 | `typhoon-v2.5-30b-a3b-instruct` ✅ | ใช้ค่าเดิมได้ (หรือ `typhoon-v2.1-12b-instruct` ตัวเล็กกว่า) |
| `server_thai_gemini.py` | 25 | `gemini-3.1-flash-lite-preview` ❌ | `gemini-2.5-flash-lite` ✅ |

ตัวอย่างการแก้ใน `server.py`:

```python
MODEL_NAME = "openai/gpt-oss-20b"
```

ดูรายการ model ที่ยังให้บริการอยู่ล่าสุดของแต่ละค่าย:

- Groq: https://console.groq.com/docs/models
- Typhoon: https://docs.opentyphoon.ai
- Gemini: https://ai.google.dev/gemini-api/docs/models

---

## ขั้นตอนที่ 5 — รันเซิร์ฟเวอร์

```bash
# อยู่ที่ Backend/rag/ (จำว่าต้อง export key ใน terminal นี้แล้ว)

# Case 1 (EN) — port 8000
uvicorn server:app --host 127.0.0.1 --port 8000

# Case 2 (TH - Typhoon) — port 8001  [เลือกอย่างใดอย่างหนึ่ง]
uvicorn server_thai:app --host 127.0.0.1 --port 8001

# Case 2 (TH - Gemini) — port 8001    [ทางเลือกแทน Typhoon]
uvicorn server_thai_gemini:app --host 127.0.0.1 --port 8001
```

เช็คว่า server พร้อมแล้ว: เปิด http://127.0.0.1:8000/docs (หรือ 8001) ในเบราว์เซอร์ ถ้าเห็นหน้า FastAPI แปลว่าใช้ได้ ✅

> จะรันทั้ง 2 คดีพร้อมกัน (2 terminal) หรือใช้สคริปต์ช่วย `./start_servers.sh` (ถาม key แล้วเปิด 2 หน้าต่างให้เลย) หรือ `./start_servers_tmux.sh` (สำหรับคนใช้ tmux) ก็ได้

---

## ขั้นตอนที่ 6 — เข้าเกม (Unity)

1. เปิดโฟลเดอร์โปรเจกต์ด้วย **Unity Hub** → เลือก Unity เวอร์ชัน **6000.1.13f1**
2. รอโปรเจกต์โหลดเสร็จ แล้วกด **▶ Play**
3. ที่ **Main Menu** กด Start → เลือกคดี:
   - **Case 1 (English)** → ต่อกับ server port **8000**
   - **Case 2 (ไทย)** → ต่อกับ server port **8001**
4. ถ้าต้องการเปลี่ยนที่อยู่ server (เช่น รัน server บนเครื่องอื่น) — แก้ที่ GameObject ที่มี `MainMenuManager` ใน Inspector ช่อง **English Server URL** / **Thai Server URL** (ค่าเริ่มต้น: `http://127.0.0.1:8000` และ `http://127.0.0.1:8001`)

---

## 🎮 วิธีเล่น (ย่อ)

1. **สำรวจ** บ้านของผู้ตาย — เดินสำรวจและเก็บหลักฐานที่พบ
2. **สอบปากคำ** พยานและผู้ต้องสงสัย — NPC ทุกตัวเริ่มจากการ **โกหก** มีความจำการสนทนา (จำสิ่งที่คุณถามไปแล้ว) และมีนิสัยตามตัวละคร
3. **ใช้หลักฐานเผชิญหน้า** — ในหน้าต่างสนทนา กดเลือกหลักฐานที่ขัดแย้งกับคำให้การ NPC ที่ถูกไขจะเปลี่ยนไปเปิดเผยความจริง
4. **อย่าใช้คำหยาบคาย ข่มขู่ หรือบังคับ** — จะถูกประเมินว่าไม่มีมืออาชีพและ **Auto Fail** ทันที
5. **ส่งรายงานสรุปคดี (Investigation Report)** — ระบุ ฆาตกร / แรงจูงใจ / วิธีฆ่า พร้อมหลักฐานและพยานบัติสนับสนุน ระบบจะให้คะแนน 0–100 พร้อม feedback

---

## 🛠️ แก้ปัญหาที่พบบ่อย

| อาการ | สาเหตุ / วิธีแก้ |
|--------|------------------|
| `404 model_not_found` / `model_decommissioned` | Model ที่ตั้งไว้หมดอายุแล้ว → เปลี่ยน `MODEL_NAME` เป็นตัวที่ยังให้บริการ (ขั้นตอน 4.2) |
| `401 invalid_api_key` | Key ผิด หรือลืม `export` ใน terminal ที่รัน server (env var ไม่ทำงานข้าม terminal) |
| เกมบอกว่าต่อ server ไม่ได้ | ลืมรัน server, port ไม่ตรงกับ URL ในเมนู, หรือกรอก key ไม่ครบ |
| NPC ตอบแต่ error เกี่ยวกับ collection / ChromaDB | ลืมสร้าง Vector DB → รัน `create_knowledge_base.py` / `create_knowledge_base_thai.py` (ขั้นตอน 2) |
| `Address already in use` | Port ชนกับ process เดิม → `lsof -ti:8000 \| xargs kill -9` (เปลี่ยนเลข port ตามจริง) |
| ตอนสร้าง Vector DB ค้าง/ช้า | กำลังดาวน์โหลด embedding model ครั้งแรก — รอจนเสร็จ (ต้องต่ออินเทอร์เน็ต) |

---

## 🗂️ โครงสร้างโปรเจกต์ (ส่วนที่เกี่ยวกับการรัน)

```text
SeniorProject/
├── Assets/                        # Unity 6 (URP, C#, TextMeshPro)
│   ├── Scene/                     # CrimeSceneLevel (Case 1), CrimeSceneLevel2 (Case 2)
│   └── Script/
│       ├── MainMenuManager.cs     # เมนูเลือกคดี + URL ของ server
│       └── LLMClientSimple.cs     # เชื่อมต่อ FastAPI backend
├── Backend/rag/
│   ├── server.py                  # 🇬🇧 Case 1 — Groq (port 8000)
│   ├── server_thai.py             # 🇹🇭 Case 2 — Typhoon (port 8001)
│   ├── server_thai_gemini.py      # 🇹🇭 Case 2 ทางเลือก — Gemini (port 8001)
│   ├── create_knowledge_base.py       # สร้าง game_db/ (EN)
│   ├── create_knowledge_base_thai.py  # สร้าง game_db_thai/ (TH)
│   ├── requirements.txt
│   └── start_servers.sh           # รัน server ทั้ง 2 คดีพร้อมกัน
└── ProjectSettings/
```

---

## 🎥 วิดีโอนำเสนอโครงงาน

📺 https://youtu.be/ox156zzYJ14?si=GkAbxzXBSykhsNfm

---

## 🎓 ข้อมูลโครงงาน

**Senior Project** — Faculty of Information and Communication Technology (ICT), Mahidol University

โปรเจกต์นี้พัฒนาเพื่อการศึกษา (Senior Project) — All rights reserved.
