using UnityEngine;

public class Billboard : MonoBehaviour
{
    Transform cam;

    void LateUpdate()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam == null) return;

        // Face the camera (no roll)
        Vector3 fwd = cam.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = cam.forward; // fallback if top-down
        transform.forward = fwd;
    }
}

