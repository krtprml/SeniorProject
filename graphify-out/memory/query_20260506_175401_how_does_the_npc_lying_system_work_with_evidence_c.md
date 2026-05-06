---
type: "query"
date: "2026-05-06T17:54:01.686967+00:00"
question: "How does the NPC lying system work with evidence confrontation?"
contributor: "graphify"
source_nodes: ["npc_has_truth build_npc_prompt StandardNPC OnEvidenceChosen UseEvidence"]
---

# Q: How does the NPC lying system work with evidence confrontation?

## Answer

The NPC lying system uses a 4-stage flow: 1) Initially NPCs lie about their conflicts with victim (EDWARD-firing, BRIAN-debts, ANNA-will, CHARLES-jealousy, DANA-promotion); 2) Player collects evidence; 3) Player confronts NPC via evidence button which calls POST /use-evidence storing evidence in state['evidence_used'][npc]; 4) npc_has_truth() checks if evidence with non-empty conflict field was used - if True, NPC tells truth about conflict, if False, NPC continues lying. Key files: server.py:523 (npc_has_truth), server.py:158 (build_npc_prompt with truth rules), StandardNPC.cs:94 (OnEvidenceChosen), LLMClientSimple.cs:124 (UseEvidence).

## Source Nodes

- npc_has_truth build_npc_prompt StandardNPC OnEvidenceChosen UseEvidence