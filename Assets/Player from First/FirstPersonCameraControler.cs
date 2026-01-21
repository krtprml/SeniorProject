using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
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
        // Get the Cinemachine Camera component
        cinemachineCamera = GetComponent<CinemachineCamera>();

        // Get the Pan Tilt component
        panTilt = GetComponent<CinemachinePanTilt>();

        if (panTilt == null)
        {
            Debug.LogError("CinemachinePanTilt component not found! Please add it to the camera or change rotation control to 'None'.");
        }

        // Find the PlayerInput (should be on the player)
        playerInput = FindFirstObjectByType<PlayerInput>();

        if (playerInput != null)
        {
            lookAction = playerInput.actions["Look"];
            // Get the player transform (the one with PlayerInput)
            playerTransform = playerInput.transform;
        }

        if (playerInput == null)
        {
            Debug.LogError("PlayerInput not found! Make sure the player has a PlayerInput component.");
        }
    }

    void OnEnable()
    {
        if (lookAction != null)
            lookAction.Enable();
    }

    void OnDisable()
    {
        if (lookAction != null)
            lookAction.Disable();
    }

    void Start()
    {
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize rotation based on current transform
        if (playerTransform != null)
        {
            yRotation = playerTransform.eulerAngles.y;
        }
    }

    void Update()
    {
        if (lookAction != null && panTilt != null)
        {
            HandleMouseLook();
        }

        // Toggle cursor lock with Escape key
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleCursorLock();
        }
    }

    void HandleMouseLook()
    {
        // Get mouse input
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();

        // Apply sensitivity
        mouseDelta *= mouseSensitivity * Time.deltaTime * 50f;

        // Horizontal rotation (Pan) - this also rotates the player
        yRotation += mouseDelta.x;

        // Vertical rotation (Tilt) - only the camera
        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // Apply rotation to Pan Tilt component
        panTilt.PanAxis.Value = yRotation;
        panTilt.TiltAxis.Value = xRotation;

        // Rotate the player's Y-axis to match the camera's horizontal rotation
        // This makes movement follow where the player is looking
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

    // Public method to set sensitivity from UI or other scripts
    public void SetSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }

    // Public getters for debugging
    public float CurrentYRotation => yRotation;
    public float CurrentXRotation => xRotation;
}