using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask = -1;

    [Header("Audio")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] footstepClips;

    // Components
    private CharacterController characterController;
    private PlayerInput playerInput;

    // Input
    private InputAction moveAction;
    private InputAction runAction;

    // Movement
    private Vector2 inputVector;
    private Vector3 velocity;
    private Vector3 currentMovement;
    private bool isGrounded;
    private bool isMoving;
    private bool isRunning;

    // Footsteps
    private float footstepTimer;
    private float footstepRate = 0.5f;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // Get input actions
        moveAction = playerInput.actions["Movement"];
        runAction = playerInput.actions["Run"];
    }

    void OnEnable()
    {
        moveAction.Enable();
        runAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        runAction.Disable();
    }

    void Start()
    {
        // Create ground check if not assigned
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    void Update()
    {
        HandleGroundCheck();
        HandleInput();
        HandleMovement();
        HandleFootsteps();
    }

    void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Keep grounded
        }
    }

    void HandleInput()
    {
        inputVector = moveAction.ReadValue<Vector2>();
        isRunning = runAction.ReadValue<float>() > 0.5f;
        isMoving = inputVector.magnitude > 0.1f;
    }

    void HandleMovement()
    {
        // Calculate movement direction
        Vector3 moveDirection = (transform.right * inputVector.x + transform.forward * inputVector.y).normalized;

        // Calculate target movement
        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 targetMovement = moveDirection * targetSpeed;

        // Smooth movement transition
        float lerpRate = isMoving ? acceleration : deceleration;
        currentMovement = Vector3.Lerp(currentMovement, targetMovement, lerpRate * Time.deltaTime);

        // Apply movement
        characterController.Move(currentMovement * Time.deltaTime);

        // Apply gravity
        velocity.y += Physics.gravity.y * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    void HandleFootsteps()
    {
        if (!isMoving || !isGrounded || footstepAudioSource == null || footstepClips.Length == 0)
            return;

        footstepTimer += Time.deltaTime;
        float currentFootstepRate = isRunning ? footstepRate * 0.6f : footstepRate;

        if (footstepTimer >= currentFootstepRate)
        {
            PlayFootstepSound();
            footstepTimer = 0f;
        }
    }

    void PlayFootstepSound()
    {
        if (footstepClips.Length > 0)
        {
            AudioClip clipToPlay = footstepClips[Random.Range(0, footstepClips.Length)];
            footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
            footstepAudioSource.PlayOneShot(clipToPlay);
        }
    }

    // Public getters for other systems
    public bool IsMoving => isMoving;
    public bool IsRunning => isRunning;
    public bool IsGrounded => isGrounded;
    public float MovementSpeed => currentMovement.magnitude;

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}