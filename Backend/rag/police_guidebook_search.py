#!/usr/bin/env python3
"""
Police Guidebook RAG Search Module
Provides search functionality for Thai police interrogation guidelines
"""

import chromadb
from typing import List, Dict, Optional

class PoliceGuidebookSearch:
    """Search police guidebook for relevant interrogation guidelines"""

    def __init__(self, db_path: str = "./police_guidebook_db", language: str = "thai"):
        """Initialize the search with police guidebook database

        Args:
            db_path: Path to ChromaDB database directory
            language: Language for label mappings ("thai" or "english")
        """
        self.db_path = db_path
        self.collection_name = "police_guidebook"
        self.collection = None
        self.language = language
        """Initialize the search with police guidebook database

        Args:
            db_path: Path to ChromaDB database directory
        """
        self.db_path = db_path
        self.collection_name = "police_guidebook"
        self.collection = None

        try:
            client = chromadb.PersistentClient(path=db_path)
            self.collection = client.get_collection(self.collection_name)
            print(f"✅ Police guidebook loaded: {self.collection.count()} documents")
        except Exception as e:
            print(f"⚠️  Could not load police guidebook: {e}")
            print(f"   Run 'python create_police_guidebook_db.py' first")

    def search_question_guidance(
        self,
        question: str,
        labels: Dict[str, bool],
        scores: Dict[str, int],
        n_results: int = 3
    ) -> List[Dict[str, str]]:
        """Search police guidebook for relevant guidance based on question evaluation

        Args:
            question: The player's question that was evaluated
            labels: Dictionary of evaluation labels (e.g., {"threatening": True, "professional": False})
            scores: Dictionary of evaluation scores (e.g., {"politeness": 1, "investigation": 2})
            n_results: Number of results to return

        Returns:
            List of relevant guidebook excerpts with metadata
        """
        if not self.collection:
            return []

        # Build search query from question and evaluation
        search_query = self._build_search_query(question, labels, scores)

        # Query the vector database
        results = self.collection.query(
            query_texts=[search_query],
            n_results=n_results
        )

        # Format results
        excerpts = []
        if results.get("documents") and results["documents"][0]:
            for i, doc in enumerate(results["documents"][0]):
                metadata = results["metadatas"][0][i] if results.get("metadatas") else {}
                excerpts.append({
                    "text": doc,
                    "section": metadata.get("section", "unknown"),
                    "relevance": self._calculate_relevance(labels, scores)
                })

        return excerpts

    def _build_search_query(self, question: str, labels: Dict[str, bool], scores: Dict[str, int]) -> str:
        """Build search query from question and evaluation results

        Args:
            question: The player's question
            labels: Evaluation labels
            scores: Evaluation scores

        Returns:
            Enhanced search query in Thai or English
        """
        query_parts = [question]

        # Add relevant labels to query (Thai and English translations)
        label_mappings = {
            "thai": {
                "threatening": "ข่มขู่คุกคาม",
                "professional": "มาตรฐานวิชาชีพ",
                "confrontational": "การเผชิญหน้า",
                "leading": "คำถามชี้นำ",
                "open_ended": "คำถามเปิด",
                "closed_ended": "คำถามปิด",
                "evidence_based": "ใช้หลักฐาน",
                "info_gathering": "การรวบรวมข้อมูล",
                "rapport_building": "สร้างความไว้ใจ",
                "emotional_appeal": "การใช้อารมณ์",
                "promise_of_favor": "การให้สัญญา",
                "context_required": "ต้องการบริบท"
            },
            "english": {
                "threatening": "threatening intimidation",
                "professional": "professional standards",
                "confrontational": "confrontational approach",
                "leading": "leading questions",
                "open_ended": "open-ended questions",
                "closed_ended": "closed-ended questions",
                "evidence_based": "evidence-based",
                "info_gathering": "information gathering",
                "rapport_building": "rapport building",
                "emotional_appeal": "emotional appeal",
                "promise_of_favor": "promise of favor",
                "context_required": "context required"
            }
        }

        # Use language-specific mappings
        mappings = label_mappings.get(self.language, label_mappings["thai"])

        # Add active labels to query
        for label, is_active in labels.items():
            if is_active and label in mappings:
                query_parts.append(mappings[label])

        # Add score context
        if self.language == "thai":
            if scores.get("politeness", 3) <= 1:
                query_parts.append("ความผิดพลาดในการสอบสวน")
                query_parts.append("ละเมิดจริยธรรม")

            if scores.get("investigation", 3) >= 2:
                query_parts.append("การสอบสวนที่มีประสิทธิภาพ")
        else:  # English
            if scores.get("politeness", 3) <= 1:
                query_parts.append("investigation errors")
                query_parts.append("ethical violations")

            if scores.get("investigation", 3) >= 2:
                query_parts.append("effective investigation")

        return " ".join(query_parts)

    def _calculate_relevance(self, labels: Dict[str, bool], scores: Dict[str, int]) -> str:
        """Calculate relevance score for the result

        Args:
            labels: Evaluation labels
            scores: Evaluation scores

        Returns:
            Relevance category (high/medium/low)
        """
        # High relevance: threatening or very unprofessional
        if labels.get("threatening") or scores.get("politeness", 3) == 0:
            return "high"

        # Medium relevance: problematic but not severe
        if labels.get("confrontational") or scores.get("politeness", 3) == 1:
            return "medium"

        # Low relevance: generally acceptable
        return "low"

    def get_explanation_for_evaluation(
        self,
        question: str,
        labels: Dict[str, bool],
        scores: Dict[str, int]
    ) -> str:
        """Get explanation from police guidebook for question evaluation

        Args:
            question: The player's question
            labels: Evaluation labels
            scores: Evaluation scores

        Returns:
            Explanation text in Thai or English
        """
        excerpts = self.search_question_guidance(question, labels, scores, n_results=2)

        if not excerpts:
            if self.language == "thai":
                return "ไม่พบข้อมูลที่เกี่ยวข้องในคู่มือตำรวจ"
            else:
                return "No relevant information found in police guidebook"

        # Build explanation from most relevant excerpt
        best_match = excerpts[0]

        if self.language == "thai":
            explanation = f"📖 อ้างอิงจากคู่มือตำรวจ ({best_match['section']}):\n\n"
        else:
            explanation = f"📖 Reference from police guidebook ({best_match['section']}):\n\n"

        explanation += best_match['text'][:500]  # Limit length

        if len(best_match['text']) > 500:
            explanation += "..."

        return explanation


# =========================
# STANDALONE TEST
# =========================
if __name__ == "__main__":
    # Test the search functionality
    searcher = PoliceGuidebookSearch()

    # Test case 1: Threatening question
    test_question = "ฉันจะทำร้ายครอบครัวนายถ้าไม่ยอมรับสารภาพ"
    test_labels = {"threatening": True, "professional": False, "confrontational": True}
    test_scores = {"politeness": 0, "investigation": 1}

    print("\n" + "="*60)
    print("TEST CASE 1: Threatening Question")
    print("="*60)
    print(f"Question: {test_question}")
    print(f"Labels: {test_labels}")
    print(f"Scores: {test_scores}")
    print("\n🔍 Search Results:")
    results = searcher.search_question_guidance(test_question, test_labels, test_scores)
    for i, result in enumerate(results, 1):
        print(f"\n{i}. Section: {result['section']}")
        print(f"   Text: {result['text'][:200]}...")
        print(f"   Relevance: {result['relevance']}")

    print("\n📝 Explanation:")
    explanation = searcher.get_explanation_for_evaluation(test_question, test_labels, test_scores)
    print(explanation)

    print("\n" + "="*60)
    print("✅ Test completed!")
    print("="*60)
