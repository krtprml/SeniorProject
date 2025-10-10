using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // ค้นหากล้องหลักในซีนตอนเริ่มต้น
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // คัดลอกมุมการหมุนของกล้องมาใส่ที่ Object นี้โดยตรง
            // วิธีนี้จะทำให้ Object หันหน้าไปในทิศทางเดียวกับกล้องเสมอ
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}