using UnityEngine;
using UnityEngine.InputSystem;

public class InputDebugger : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction runAction;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Movement"];
            lookAction = playerInput.actions["Look"];
            runAction = playerInput.actions["Run"];
        }
    }

    void Update()
    {
        if (moveAction != null)
        {
            Vector2 movement = moveAction.ReadValue<Vector2>();
            if (movement.magnitude > 0.1f)
            {
                Debug.Log($"Movement Input: {movement}");
            }
        }

        if (lookAction != null)
        {
            Vector2 look = lookAction.ReadValue<Vector2>();
            if (look.magnitude > 0.1f)
            {
                Debug.Log($"Look Input: {look}");
            }
        }

        if (runAction != null)
        {
            float run = runAction.ReadValue<float>();
            if (run > 0.1f)
            {
                Debug.Log($"Run Input: {run}");
            }
        }
    }
}