using UnityEngine;
using UnityEngine.InputSystem;

public class NotebookController : MonoBehaviour
{
    public GameObject notebookPanel;

    [Header("Player Lock Settings")]
    [Tooltip("Drag ONLY your Camera Look script and Movement script here.")]
    public MonoBehaviour[] playerScriptsToDisable;

    [Header("Tutorial Settings")]
    public bool isLockedOpen = true;

    // 🔥 NEW: A reference to your Evidence UI
    private EvidenceUIManager evidenceUI;

    void Start()
    {
        notebookPanel.SetActive(true);
        isLockedOpen = true;
        SetPlayerLock(true);

        // Find the EvidenceUIManager sitting on the NotebookPanel
        if (notebookPanel != null)
        {
            evidenceUI = notebookPanel.GetComponent<EvidenceUIManager>();
        }
    }

    void Update()
    {
        // Checks if the camera script stole the mouse. If it did, it steals it back! (Zero Lag)
        if (notebookPanel.activeInHierarchy)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // 🔥 THE NEW SMART TAB LOGIC 🔥
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (!isLockedOpen)
            {
                // 🔥 TRAFFIC LIGHT: If notebook is closed, make sure nothing else is open before opening!
                if (!notebookPanel.activeSelf && UIStateManager.I != null && UIStateManager.I.IsAnyBlockingUIOpen()) return;

                if (evidenceUI != null && evidenceUI.detailPanel != null && evidenceUI.detailPanel.activeInHierarchy)
                {
                    evidenceUI.CloseDetailPanel();
                }
                else
                {
                    ToggleNotebook();
                }
            }
        }
    }

    public void ToggleNotebook()
    {
        bool isOpen = !notebookPanel.activeSelf;
        notebookPanel.SetActive(isOpen);
        // 🔥 Tell the traffic light
        if (UIStateManager.I != null) UIStateManager.I.isNotebookOpen = isOpen;
        SetPlayerLock(isOpen);
    }

    private void SetPlayerLock(bool isNotebookOpen)
    {
        foreach (MonoBehaviour script in playerScriptsToDisable)
        {
            if (script != null)
            {
                script.enabled = !isNotebookOpen;
            }
        }

        if (isNotebookOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }
    }

    public void UnlockNotebook()
    {
        isLockedOpen = false;
    }
}