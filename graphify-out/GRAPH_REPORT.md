# Graph Report - .  (2026-05-06)

## Corpus Check
- 533 files · ~53,613,340 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 605 nodes · 1007 edges · 36 communities (26 shown, 10 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 21 edges (avg confidence: 0.5)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Backend Server|Backend Server]]
- [[_COMMUNITY_Case Evaluation|Case Evaluation]]
- [[_COMMUNITY_Evidence UI Manager|Evidence UI Manager]]
- [[_COMMUNITY_Unity Animation System|Unity Animation System]]
- [[_COMMUNITY_First Person Controller|First Person Controller]]
- [[_COMMUNITY_Evidence Database|Evidence Database]]
- [[_COMMUNITY_Standard NPC System|Standard NPC System]]
- [[_COMMUNITY_Input System|Input System]]
- [[_COMMUNITY_Chat Zone|Chat Zone]]
- [[_COMMUNITY_Investigation Report Form|Investigation Report Form]]
- [[_COMMUNITY_Notebook UI|Notebook UI]]
- [[_COMMUNITY_Game End Manager|Game End Manager]]
- [[_COMMUNITY_Evaluation Metrics|Evaluation Metrics]]
- [[_COMMUNITY_Camera Billboard System|Camera Billboard System]]
- [[_COMMUNITY_Main Menu|Main Menu]]
- [[_COMMUNITY_Pause Manager|Pause Manager]]
- [[_COMMUNITY_Case Evaluation Display|Case Evaluation Display]]
- [[_COMMUNITY_Editor Tools|Editor Tools]]
- [[_COMMUNITY_Notebook Controller|Notebook Controller]]
- [[_COMMUNITY_Tests|Tests]]
- [[_COMMUNITY_Dialogue Manager|Dialogue Manager]]
- [[_COMMUNITY_LLM Client|LLM Client]]
- [[_COMMUNITY_Evidence Manager|Evidence Manager]]
- [[_COMMUNITY_Game Manager|Game Manager]]
- [[_COMMUNITY_Cinemachine|Cinemachine]]
- [[_COMMUNITY_TextMeshPro|TextMeshPro]]
- [[_COMMUNITY_Input Actions|Input Actions]]

## God Nodes (most connected - your core abstractions)
1. `string` - 39 edges
2. `InvestigationReportFormTai` - 30 edges
3. `InvestigationReportForm` - 30 edges
4. `PoliceGuidebookSearch` - 28 edges
5. `GameEndManager` - 26 edges
6. `FirstPersonController` - 24 edges
7. `bool` - 22 edges
8. `StandardNPCTai` - 22 edges
9. `StandardNPC` - 22 edges
10. `CaseEvaluatorNPC` - 21 edges

## Surprising Connections (you probably didn't know these)
- `CaseEvaluatorNPC` --references--> `InvestigationReportForm`  [EXTRACTED]
  Assets/Script/CaseEvaluatorNPC.cs → Assets/Script/NotebookReportSubmitter.cs
- `InvestigationReportFormTai` --references--> `Action<InvestigationReport>`  [EXTRACTED]
  Assets/Script/InvestigationReportFormTai.cs → Assets/Script/InvestigationReportForm.cs
- `Section` --references--> `string`  [EXTRACTED]
  Assets/UnityStartups/Unity/TutorialInfo/Scripts/Readme.cs → Assets/Script/ChatMessage.cs
- `ReadmeEditor` --references--> `string`  [EXTRACTED]
  Assets/UnityStartups/Unity/TutorialInfo/Scripts/Editor/ReadmeEditor.cs → Assets/Script/ChatMessage.cs
- `CaseEvaluatorNPC` --references--> `string`  [EXTRACTED]
  Assets/Script/CaseEvaluatorNPC.cs → Assets/Script/ChatMessage.cs

## Communities (36 total, 10 thin omitted)

### Community 0 - "Backend Server"
Cohesion: 0.05
Nodes (68): BaseModel, PoliceGuidebookSearch, Search police guidebook for relevant interrogation guidelines, Initialize the search with police guidebook database          Args:, Calculate relevance score for the result          Args:             labels: Eval, Get explanation from police guidebook for question evaluation          Args:, Search police guidebook for relevant guidance based on question evaluation, Build search query from question and evaluation results          Args: (+60 more)

### Community 1 - "Case Evaluation"
Cohesion: 0.05
Nodes (41): bool, CaseInfo, CaseResult, EvidenceDisplayMode, float, int, List, CaseInfo (+33 more)

### Community 2 - "Evidence UI Manager"
Cohesion: 0.06
Nodes (18): Button, CaseEvaluationNotebookDisplay, GameObject, Image, InvestigationReportForm, NotebookController, PauseManager, EvidenceHUD (+10 more)

### Community 3 - "Unity Animation System"
Cohesion: 0.06
Nodes (12): Action, EvidenceReveal, MonoBehaviour, RectTransform, AnimatorController, AutoFailScreen, DiagnosticButton, EvidenceChoiceButton (+4 more)

### Community 4 - "First Person Controller"
Cohesion: 0.07
Nodes (13): AudioClip, AudioSource, CharacterController, CinemachineCamera, CinemachinePanTilt, InputAction, FirstPersonCameraController, FirstPersonController (+5 more)

### Community 5 - "Evidence Database"
Cohesion: 0.1
Nodes (4): Dictionary, EvidenceDatabase, EvidenceDatabaseThai, InvestigationReportFormTai

### Community 6 - "Standard NPC System"
Cohesion: 0.11
Nodes (4): EvidenceChoiceButton, StandardNPC, StandardNPCTai, ScrollRect

### Community 7 - "Input System"
Cohesion: 0.09
Nodes (12): IDisposable, IInputActionCollection2, InputActionMap, AddCallbacks(), Disable(), Enable(), Get(), IPlayerActions (+4 more)

### Community 8 - "Chat Zone"
Cohesion: 0.12
Nodes (4): EvaluatorStage, InputActionReference, CaseEvaluatorNPC, ChatZoneTrigger

### Community 10 - "Notebook UI"
Cohesion: 0.11
Nodes (5): EvidenceRowController, NotebookNotesPage, WitnessRowController, TMP_Dropdown, TMP_InputField

### Community 11 - "Game End Manager"
Cohesion: 0.18
Nodes (3): AutoFailScreen, ObjectHighlighter, GameEndManager

### Community 12 - "Evaluation Metrics"
Cohesion: 0.14
Nodes (20): calculate_boolean_accuracy(), calculate_confusion_metrics(), calculate_numeric_error(), calculate_per_question_errors(), load_ground_truth(), load_llm_results(), match_questions(), normalize_text() (+12 more)

### Community 13 - "Camera Billboard System"
Cohesion: 0.15
Nodes (7): Camera, Color, LayerMask, Material, Renderer, Billboard, ObjectHighlighter

### Community 17 - "Editor Tools"
Cohesion: 0.27
Nodes (3): Editor, ReadmeEditor, GUIStyle

### Community 21 - "LLM Client"
Cohesion: 0.36
Nodes (7): add_to_memory(), build_evaluator_prompt(), build_system_prompt(), chat(), evaluate_question(), get_recent_memory(), PlayerRequest

### Community 23 - "Game Manager"
Cohesion: 0.67
Nodes (3): build_prompt(), evaluate_batch(), Evaluate a batch of questions with a specific model

### Community 24 - "Cinemachine"
Cohesion: 0.83
Nodes (3): build_prompt(), evaluate_question(), run_model()

## Knowledge Gaps
- **42 isolated node(s):** `Texture2D`, `Section`, `GUIStyle`, `EvaluatorStage`, `CaseResult` (+37 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `string` connect `Case Evaluation` to `Evidence UI Manager`, `Unity Animation System`, `Standard NPC System`, `Chat Zone`, `Notebook UI`, `Game End Manager`, `Main Menu`, `Pause Manager`, `Case Evaluation Display`, `Editor Tools`, `Tests`, `Dialogue Manager`, `Evidence Manager`?**
  _High betweenness centrality (0.124) - this node is a cross-community bridge._
- **Why does `@PlayerInputActions` connect `Input System` to `Case Evaluation`, `First Person Controller`?**
  _High betweenness centrality (0.066) - this node is a cross-community bridge._
- **Why does `GameObject` connect `Evidence UI Manager` to `Unity Animation System`, `Evidence Database`, `Standard NPC System`, `Chat Zone`, `Investigation Report Form`, `Game End Manager`, `Camera Billboard System`, `Main Menu`, `Pause Manager`, `Case Evaluation Display`, `Notebook Controller`, `Tests`?**
  _High betweenness centrality (0.060) - this node is a cross-community bridge._
- **Are the 21 inferred relationships involving `PoliceGuidebookSearch` (e.g. with `PlayerRequest` and `EvidenceRequest`) actually correct?**
  _`PoliceGuidebookSearch` has 21 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Texture2D`, `Section`, `GUIStyle` to the rest of the system?**
  _42 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Backend Server` be split into smaller, more focused modules?**
  _Cohesion score 0.05 - nodes in this community are weakly interconnected._
- **Should `Case Evaluation` be split into smaller, more focused modules?**
  _Cohesion score 0.05 - nodes in this community are weakly interconnected._