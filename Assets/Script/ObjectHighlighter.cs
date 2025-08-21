using UnityEngine;

public class ObjectHighlighter : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Camera playerCamera;
    public float maxRaycastDistance = 100f;
    public LayerMask highlightLayerMask = -1; // All layers by default

    [Header("Highlight Settings")]
    public Material highlightMaterial;
    public Color highlightColor = Color.yellow;
    public float highlightIntensity = 2f;

    private GameObject currentHighlightedObject;
    private Renderer currentRenderer;
    private Material[] originalMaterials;
    private Material[] highlightMaterials;

    void Start()
    {
        // If no camera is assigned, try to find the main camera
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError("No camera found! Please assign a camera to the ObjectHighlighter script.");
            enabled = false;
        }

        // Create highlight material if none is provided
        if (highlightMaterial == null)
        {
            highlightMaterial = CreateHighlightMaterial();
        }
    }

    void Update()
    {
        CheckForHighlightableObject();
    }

    void CheckForHighlightableObject()
    {
        // Cast ray from center of screen
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.red, 0.1f);

        if (Physics.Raycast(ray, out hit, maxRaycastDistance, highlightLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"Raycast hit: {hitObject.name}");

            // Check if the object has a HighlightableObject component
            HighlightableObject highlightable = hitObject.GetComponent<HighlightableObject>();

            if (highlightable != null && highlightable.canBeHighlighted)
            {
                Debug.Log($"Object {hitObject.name} is highlightable");
                // If this is a new object, highlight it
                if (currentHighlightedObject != hitObject)
                {
                    RemoveHighlight();
                    HighlightObject(hitObject);
                }
            }
            else
            {
                Debug.Log($"Object {hitObject.name} is not highlightable or component missing");
                // Hit object is not highlightable, remove current highlight
                RemoveHighlight();
            }
        }
        else
        {
            // No object hit, remove current highlight
            RemoveHighlight();
        }
    }

    void HighlightObject(GameObject obj)
    {
        currentHighlightedObject = obj;
        currentRenderer = obj.GetComponent<Renderer>();

        if (currentRenderer == null)
        {
            Debug.LogWarning($"No Renderer found on {obj.name}!");
            return;
        }

        Debug.Log($"Highlighting object: {obj.name}");

        // Store original materials
        originalMaterials = currentRenderer.materials;

        // Create highlight materials array
        highlightMaterials = new Material[originalMaterials.Length];

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            highlightMaterials[i] = highlightMaterial;
        }

        // Apply highlight materials
        currentRenderer.materials = highlightMaterials;

        // Notify the highlightable object
        HighlightableObject highlightable = obj.GetComponent<HighlightableObject>();
        if (highlightable != null)
        {
            highlightable.OnHighlightEnter();
        }
    }

    void RemoveHighlight()
    {
        if (currentHighlightedObject != null && currentRenderer != null)
        {
            // Restore original materials
            currentRenderer.materials = originalMaterials;

            // Notify the highlightable object
            HighlightableObject highlightable = currentHighlightedObject.GetComponent<HighlightableObject>();
            if (highlightable != null)
            {
                highlightable.OnHighlightExit();
            }

            currentHighlightedObject = null;
            currentRenderer = null;
            originalMaterials = null;
            highlightMaterials = null;
        }
    }

    Material CreateHighlightMaterial()
    {
        // Try different shaders based on render pipeline
        Material mat;

        // Try URP first, then HDRP, then fallback to Standard
        if (Shader.Find("Universal Render Pipeline/Lit") != null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", highlightColor);
            mat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            mat.EnableKeyword("_EMISSION");
        }
        else if (Shader.Find("HDRP/Lit") != null)
        {
            mat = new Material(Shader.Find("HDRP/Lit"));
            mat.SetColor("_BaseColor", highlightColor);
            mat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            mat.EnableKeyword("_EMISSION");
        }
        else
        {
            // Fallback to Standard/Built-in
            mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", highlightColor);
            mat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        // Make it unlit and bright
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.5f);

        Debug.Log($"Created highlight material with shader: {mat.shader.name}");
        return mat;
    }

    void OnDrawGizmos()
    {
        // Draw raycast line in scene view for debugging
        if (playerCamera != null)
        {
            Vector3 rayStart = playerCamera.transform.position;
            Vector3 rayDirection = playerCamera.transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayStart, rayStart + rayDirection * maxRaycastDistance);
        }
    }
}
