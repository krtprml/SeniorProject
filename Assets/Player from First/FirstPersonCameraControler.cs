using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 1.5f; // You may need to slightly adjust this in the Inspector now!
    [SerializeField] private float maxLookAngle = 80f;

    // Components
    private PlayerInput playerInput;
    private CinemachineCamera cinemachineCamera;
    private CinemachinePanTilt panTilt;
    private Transform playerTransform; // Reference to the player

    // Input
    private InputAction lookAction;

    // Mouse look values
    private float xRotation = 0f;
    private float yRotation = 0f;

    void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
        panTilt = GetComponent<CinemachinePanTilt>();

        if (panTilt == null)
            Debug.LogError("CinemachinePanTilt component not found!");

        playerInput = FindFirstObjectByType<PlayerInput>();

        if (playerInput != null)
        {
            lookAction = playerInput.actions["Look"];
            playerTransform = playerInput.transform;
        }
        else
        {
            Debug.LogError("PlayerInput not found! Make sure the player has a PlayerInput component.");
        }
    }

    void OnEnable()
    {
        if (lookAction != null) lookAction.Enable();
    }

    void OnDisable()
    {
        if (lookAction != null) lookAction.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerTransform != null)
        {
            yRotation = playerTransform.eulerAngles.y;
        }
    }

    void Update()
    {
        // 🔥 FIX: Only read mouse input if the cursor is actually locked into the game
        if (lookAction != null && panTilt != null && Cursor.lockState == CursorLockMode.Locked)
        {
            HandleMouseLook();
        }

        // ❌ We deleted the hardcoded Escape key check here! 
        // Let PauseManager and the NPCs handle the Escape key instead.
    }

    void HandleMouseLook()
    {
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();

        // 🔥 FIX: Removed Time.deltaTime! Mouse delta is already frame-independent.
        mouseDelta *= mouseSensitivity * 0.1f;

        yRotation += mouseDelta.x;
        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        panTilt.PanAxis.Value = yRotation;
        panTilt.TiltAxis.Value = xRotation;

        if (playerTransform != null)
        {
            playerTransform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }

    void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }

    public float CurrentYRotation => yRotation;
    public float CurrentXRotation => xRotation;
}