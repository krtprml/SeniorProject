---
type: "query"
date: "2026-05-06T18:13:51.658008+00:00"
question: "Compare RAG flow between English server.py and Thai servers (server_thai.py, server_thai_gemini.py)"
contributor: "graphify"
source_nodes: ["chat npc_has_truth murder_collection server_thai.py server_thai_gemini.py"]
---

# Q: Compare RAG flow between English server.py and Thai servers (server_thai.py, server_thai_gemini.py)

## Answer

RAG flow is IDENTICAL across all three versions: ChromaDB PersistentClient, murder_collection.query() with where={'owner': npc}, n_results=5, context assembly. Differences: server.py uses ./game_db/murder_case + Groq llama-3.1-8b, server_thai.py uses ./game_db_thai/murder_case_thai + Typhoon API typhoon-v2.5-30b, server_thai_gemini.py uses ./game_db_thai/murder_case_thai + Gemini gemini-3.1-flash-lite. Truth-telling system (npc_has_truth) is identical - all check evidence_used[npc] for non-empty conflict field.

## Source Nodes

- chat npc_has_truth murder_collection server_thai.py server_thai_gemini.py