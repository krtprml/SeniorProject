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

    void Start()
    {
        notebookPanel.SetActive(true);
        isLockedOpen = true;
        SetPlayerLock(true);
    }

    void Update()
    {
        // 🔥 THE SMART MOUSE ENFORCER: 
        // Checks if the camera script stole the mouse. If it did, it steals it back! (Zero Lag)
        if (notebookPanel.activeInHierarchy)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // Handle the TAB key
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (!isLockedOpen)
            {
                ToggleNotebook();
            }
        }
    }

    public void ToggleNotebook()
    {
        bool isOpen = !notebookPanel.activeSelf;
        notebookPanel.SetActive(isOpen);
        SetPlayerLock(isOpen);
    }

    private void SetPlayerLock(bool isNotebookOpen)
    {
        // Disable or enable the scripts
        foreach (MonoBehaviour script in playerScriptsToDisable)
        {
            if (script != null)
            {
                script.enabled = !isNotebookOpen;
            }
        }

        // Initial setup for pausing/unpausing
        if (isNotebookOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f; // Pause the world safely
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f; // Unpause the world
        }
    }

    public void UnlockNotebook()
    {
        isLockedOpen = false;
    }
}