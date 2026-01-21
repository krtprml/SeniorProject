using UnityEngine;
using UnityEngine.InputSystem;

public class ChatZoneTrigger : MonoBehaviour
{
    [Header("UI")]
    public GameObject chatCanvas; 
    public GameObject chatPanel;  

    [Header("Input Actions")]
    public InputActionReference interactAction; // ปุ่ม E
    public InputActionReference cancelAction;   // ปุ่ม Esc

    private bool playerInZone = false;
    private bool panelOpen = false;

    void Start()
    {
        if (chatCanvas) chatCanvas.SetActive(false);
        if (chatPanel) chatPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (interactAction)
            interactAction.action.performed += OnInteract;
        if (cancelAction)
            cancelAction.action.performed += OnCancel;
    }

    void OnDisable()
    {
        if (interactAction)
            interactAction.action.performed -= OnInteract;
        if (cancelAction)
            cancelAction.action.performed -= OnCancel;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            if (chatCanvas) chatCanvas.SetActive(true); // เปิด Canvas แต่ยังไม่เปิด Panel
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            if (chatCanvas) chatCanvas.SetActive(false);
            if (chatPanel) chatPanel.SetActive(false);
            panelOpen = false;
            Time.timeScale = 1f; // กลับมาเล่นเกมต่อถ้าออกโซน
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (playerInZone && !panelOpen)
        {
            panelOpen = true;
            if (chatPanel) chatPanel.SetActive(true);
            Time.timeScale = 0f; // หยุดเกม
        }
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (panelOpen)
        {
            panelOpen = false;
            if (chatPanel) chatPanel.SetActive(false);
            Time.timeScale = 1f; // เล่นเกมต่อ
        }
    }
}