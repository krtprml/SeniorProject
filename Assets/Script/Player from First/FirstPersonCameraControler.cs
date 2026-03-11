using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Tight Smoothing")]
    [Tooltip("Keep this VERY low. 0.01 to 0.03 is the sweet spot for crisp but smooth movement.")]
    [SerializeField] private float smoothTime = 0.02f;

    // Components
    private PlayerInput playerInput;
    private CinemachineCamera cinemachineCamera;
    private CinemachinePanTilt panTilt;
    private Transform playerTransform;

    // Input
    private InputAction lookAction;

    // Actual rotations applied to the camera
    private float xRotation = 0f;
    private float yRotation = 0f;

    // The smoothed mouse speed
    private Vector2 currentMouseDelta;
    private Vector2 currentMouseVelocity;

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
        if (lookAction != null && panTilt != null && Cursor.lockState == CursorLockMode.Locked)
        {
            HandleMouseLook();
        }
    }

    void HandleMouseLook()
    {
        // 1. Get the raw, snappy mouse movement
        Vector2 targetMouseDelta = lookAction.ReadValue<Vector2>();

        // 2. Smooth the SPEED of the mouse, not the camera position!
        currentMouseDelta = Vector2.SmoothDamp(
            currentMouseDelta,
            targetMouseDelta,
            ref currentMouseVelocity,
            smoothTime
        );

        // 3. Apply sensitivity to the smoothed speed
        Vector2 finalDelta = currentMouseDelta * mouseSensitivity * 0.1f;

        // 4. Add it to our rotation
        yRotation += finalDelta.x;
        xRotation -= finalDelta.y;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // 5. Apply the crisp rotation instantly
        panTilt.PanAxis.Value = yRotation;
        panTilt.TiltAxis.Value = xRotation;

        if (playerTransform != null)
        {
            playerTransform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }

    public void SetSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }

    public float CurrentYRotation => yRotation;
    public float CurrentXRotation => xRotation;
}