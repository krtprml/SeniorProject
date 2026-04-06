import streamlit as st
import json
import random
import os

st.set_page_config(page_title="Police Assessment Tool", layout="wide")

# --- 1. ตั้งค่าชุด Label ---
LABEL_OPTIONS = {
    "📌 1. รูปแบบคำถาม": {
        "open_ended": "คำถามปลายเปิด (ให้เล่าเรื่อง)",
        "closed_ended": "คำถามปลายปิด (ตอบสั้นๆ)",
        "leading": "คำถามชี้นำ (ยัดเยียดคำตอบ)"
    },
    "🔍 2. กลยุทธ์/เจตนา": {
        "info_gathering": "หาข้อมูลใหม่",
        "evidence_based": "อ้างหลักฐาน/ตรวจสอบ",
        "rapport_building": "สร้างความสัมพันธ์",
        "confrontational": "กดดัน/จี้จุด"
    },
    "⚖️ 3. พฤติกรรม/โทน": {
        "professional": "สุภาพ/มืออาชีพ",
        "threatening": "ข่มขู่/คุกคาม",
        "emotional_appeal": "ใช้จิตวิทยา/อารมณ์",
        "promise_of_favor": "ให้สัญญา/ยื่นข้อเสนอ"
    },
    "📂 4. อื่นๆ": {
        "context_required": "ตัดสินไม่ได้/ต้องมีบริบท"
    }
}

SCORING_CRITERIA = {
    "politeness": {
        "label": "🛡️ ความสุภาพ / มาตรฐานวิชาชีพ",
        "options": [
            "0 : ไม่เป็นมืออาชีพ ใช้ความรุนแรง หรือข่มขู่",
            "1 : ไม่เหมาะสม ก้าวร้าว หรือมีอคติ",
            "2 : ยอมรับได้แต่ไม่สมบูรณ์",
            "3 : เป็นมืออาชีพ สงบ เคารพจริยธรรม"
        ]
    },
    "investigation": {
        "label": "🚀 คุณภาพการสืบสวน",
        "options": [
            "0 : ไม่เกี่ยวข้อง เป็นอันตราย หรือขัดขวาง",
            "1 : เทคนิคแย่ ใช้คำถามยัดเยียด หรือเสี่ยง",
            "2 : เกี่ยวข้องแต่อ่อน กำกวม หรือไม่มีประสิทธิภาพ",
            "3 : ใช้หลักฐาน เกี่ยวข้อง ขับเคลื่อนการสืบสวน"
        ]
    }
}

# --- 2. การจัดการข้อมูล (โหลดจากไฟล์ TXT) ---
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# --- 3. ฟังก์ชันโหลดไฟล์ ---
def load_questions():
    # สร้าง Path แบบ Dynamic โดยอ้างอิงจาก Base Directory
    file_path = os.path.join(BASE_DIR, "Backend", "rag", "questions_thai.txt")
    
    if os.path.exists(file_path):
        with open(file_path, "r", encoding="utf-8") as f:
            lines = [line.strip() for line in f.readlines() if line.strip()]
            return lines
    else:
        # ใช้ st.warning แทน st.error เพื่อความปลอดภัยในการรันช่วงแรก
        st.warning(f"⚠️ ไม่พบไฟล์ที่พาธ: {file_path}")
        return ["ไม่พบข้อมูลประโยคในไฟล์ กรุณาเช็ค Path"]

# --- 4. การจัดการข้อมูล (Session State) ---
if 'sentences' not in st.session_state:
    loaded_data = load_questions()
    st.session_state.sentences = loaded_data
    st.session_state.results = []
    st.session_state.current_index = 0

# --- 3. UI Setup ---


# (ส่วน Expander แฟ้มคดี เหมือนเดิม...)
with st.expander("📁 เปิดแฟ้มข้อมูลคดีฆาตกรรม นายวิชาญ ศรีวัฒน์ (ความจริงสัมบูรณ์)"):
    tab1, tab2, tab3 = st.tabs(["📋 สรุปสำนวน", "👥 ข้อมูลพยาน/ผู้ต้องสงสัย", "⏳ Timeline จริง"])
    with tab1:
        st.markdown("""
        **ผู้ตาย:** นายวิชาญ ศรีวัฒน์ (68 ปี) | **สาเหตุ:** ขาดอากาศหายใจ (ผูกคอ) โดยถูกวางยาก่อน
        - **ฆาตกร:** นายชัยวัฒน์ (หุ้นส่วนบัญชี)
        - **แรงจูงใจ:** ยักยอกเงินบริษัท (ผู้ตายจับได้และกำลังจะแจ้งความ/แก้ไขพินัยกรรม)
        - **วิธีการ:** 1. **วางยา:** ใส่ยานอนหลับบดในแก้วน้ำชาที่ครัว (21:02 น.)
            2. **อำพราง:** จัดฉากผูกคอกับคานขณะหมดสติเพื่อให้ดูเหมือนฆ่าตัวตาย (ไม่มีรอยดิ้นรน)
            3. **ห้องปิดตาย:** ใช้ 'เส้นเอ็นตกปลา' ผูกกลอนประตู ลากลอดใต้ช่องประตูแล้วดึงล็อกจากด้านนอกก่อนดึงสายเอ็นกลับ
        """)
    with tab2:
        col_a, col_b = st.columns(2)
        with col_a:
            st.error("**ชัยวัฒน์ (ฆาตกร):** วางยาและจัดฉากห้องปิดตาย แสร้งว่ากลับบ้านตอน 20:50 น.")
            st.info("**สมหญิง (แม่บ้าน):** พยานสำคัญ เห็นชัยวัฒน์ที่ถาดชาตอน 21:02 น. และแอบขึ้นชั้น 2 ตอน 21:10 น.")
        with col_b:
            st.success("**เมษา (ลูกสาว):** ผู้แจ้งเบาะแสเรื่องยักยอกเงิน ไม่มีเหตุจูงใจฆ่าเพราะจะได้มรดก 60% พรุ่งนี้")
            st.success("**พรทิพย์ (ภรรยา) & ธันวา (ลูกชาย):** มีความขัดแย้งแต่ Timeline และวิธีการไม่สอดคล้องกับการฆาตกรรม")
    with tab3:
        st.write("⏱️ **ลำดับเหตุการณ์จริง:**")
        st.code("""
        20:45 - เมษาออกจากห้องทำงาน / ชัยวัฒน์เข้าไปเผชิญหน้า
        20:50 - ชัยวัฒน์โกหกว่าจะกลับบ้าน แต่แอบไปกบดาน
        21:02 - ชัยวัฒน์วางยาในแก้วชาที่ครัว (สมหญิงเห็น)
        21:05 - ชัยวัฒน์นำชาขึ้นไปให้ผู้ตาย / ผู้ตายดื่มแล้วเริ่มหมดสติ
        21:12 - ชัยวัฒน์ลงมือสังหารและทำกลไกเอ็นดึงกลอนประตู (ห้องปิดตาย)
        21:15 - ชัยวัฒน์หลบออกจากบ้านเงียบๆ
        21:30 - พรทิพย์ขึ้นไปพบว่าประตูล็อก (แจ้งเหตุ)
        """)

# ส่วนหัว
head_col1, head_col2 = st.columns([5, 1])
with head_col1:
    st.subheader("🛡️ ระบบประเมินการสอบสวน")
with head_col2:
    st.markdown(f"### 📍 {st.session_state.current_index + 1} / {len(st.session_state.sentences)}")

# ตรวจสอบสถานะและแสดงผล Form
if st.session_state.current_index < len(st.session_state.sentences):
    current_sentence = st.session_state.sentences[st.session_state.current_index]
    st.info(f"👉 **ประโยค:** {current_sentence}")

    with st.form(key=f'form_{st.session_state.current_index}'):
        col_score, col_label = st.columns([1, 1.8])

        with col_score:
            st.markdown("### 📊 ให้คะแนน")
            p_score = st.radio(
                SCORING_CRITERIA["politeness"]["label"],
                options=range(4),
                format_func=lambda x: SCORING_CRITERIA["politeness"]["options"][x],
                index=3,
                key=f"p_{st.session_state.current_index}"
            )
            i_score = st.radio(
                SCORING_CRITERIA["investigation"]["label"],
                options=range(4),
                format_func=lambda x: SCORING_CRITERIA["investigation"]["options"][x],
                index=3,
                key=f"i_{st.session_state.current_index}"
            )

        with col_label:
            st.markdown("### 🏷️ ระบุลักษณะ")
            selected_keys = []
            sub_col1, sub_col2 = st.columns(2)
            groups = list(LABEL_OPTIONS.keys())
            
            for idx, group_name in enumerate(groups):
                target_sub_col = sub_col1 if idx < 2 else sub_col2
                with target_sub_col:
                    st.markdown(f"**{group_name}**")
                    for label_eng, label_tha in LABEL_OPTIONS[group_name].items():
                        if target_sub_col.checkbox(label_tha, key=f"cb_{st.session_state.current_index}_{label_eng}"):
                            selected_keys.append(label_eng)
                    st.write("") 

        submit_button = st.form_submit_button(label='✅ บันทึกและถัดไป', use_container_width=True)

    if submit_button:
        all_labels_eng = {eng: (eng in selected_keys) for group in LABEL_OPTIONS.values() for eng in group.keys()}
        st.session_state.results.append({
            "sentence": current_sentence,
            "scoring": {"politeness": p_score, "investigation": i_score},
            "labels": all_labels_eng
        })
        st.session_state.current_index += 1
        st.rerun()

else:
    st.balloons()
    st.success("✅ ประเมินครบเรียบร้อย!")
    st.download_button("📥 Download JSON (English Keys)", json.dumps(st.session_state.results, ensure_ascii=False, indent=2), "output.json", use_container_width=True)
    st.json(st.session_state.results)