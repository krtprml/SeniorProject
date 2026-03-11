using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Smoothing Settings")]
    [Tooltip("How much 'glide' the camera has. Lower is snappier, higher is floatier. 0.05 is a good default.")]
    [SerializeField] private float smoothTime = 0.05f;

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

    // Target rotations (where the mouse actually is)
    private float targetX = 0f;
    private float targetY = 0f;

    // Velocity references used by the smoothing math
    private float xRotationVelocity;
    private float yRotationVelocity;

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
            // Sync both actual and target rotations at the start
            yRotation = playerTransform.eulerAngles.y;
            targetY = yRotation;
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
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
        mouseDelta *= mouseSensitivity * 0.1f;

        // 1. Calculate where the camera WANTS to be (Target)
        targetY += mouseDelta.x;
        targetX -= mouseDelta.y;
        targetX = Mathf.Clamp(targetX, -maxLookAngle, maxLookAngle);

        // 2. Smoothly glide the ACTUAL rotation towards the TARGET rotation
        xRotation = Mathf.SmoothDampAngle(xRotation, targetX, ref xRotationVelocity, smoothTime);
        yRotation = Mathf.SmoothDampAngle(yRotation, targetY, ref yRotationVelocity, smoothTime);

        // 3. Apply the smoothed rotations
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