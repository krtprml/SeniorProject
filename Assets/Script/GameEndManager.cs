using UnityEngine;
using UnityEngine.SceneManagement; // สำคัญมาก! สำหรับการโหลดซีน

public class GameEndManager : MonoBehaviour
{
    // สร้าง Singleton เพื่อให้เรียกใช้จากสคริปต์อื่นได้ง่าย
    public static GameEndManager instance; 

    [Header("UI Screens")]
    public GameObject winScreenUI;
    public GameObject loseScreenUI;

    [Header("Player Controller")]
    // ลาก Player ที่มีสคริปต์ ObjectHighlighter มาใส่
    public ObjectHighlighter playerController; 

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    // ฟังก์ชันหลักที่จะถูกเรียกจากสคริปต์ NPC
    public void ShowEndScreen(bool didWin)
    {
        // ปิดการควบคุมของผู้เล่นและแสดง Cursor
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // แสดงหน้าจอที่ถูกต้อง
        if (didWin)
        {
            winScreenUI.SetActive(true);
        }
        else
        {
            loseScreenUI.SetActive(true);
        }
    }

    // ฟังก์ชันสำหรับปุ่ม Restart
    public void RestartGame()
    {
        // โหลดซีนปัจจุบันใหม่
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    // ฟังก์ชันสำหรับปุ่ม Quit
    public void QuitGame()
    {
        Application.Quit();
    }
}