using UnityEngine;
using UnityEngine.InputSystem; // 🔥 REQUIRED for New Input System

public class ObjectHighlighter : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Camera playerCamera;
    public float maxRaycastDistance = 100f;
    public LayerMask highlightLayerMask = -1;

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
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (highlightMaterial == null)
            highlightMaterial = CreateHighlightMaterial();
    }

    void Update()
    {
        CheckForHighlightableObject();
    }

    void CheckForHighlightableObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRaycastDistance, highlightLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            HighlightableObject highlightable = hitObject.GetComponent<HighlightableObject>();

            if (highlightable != null && highlightable.canBeHighlighted)
            {
                if (currentHighlightedObject != hitObject)
                {
                    RemoveHighlight();
                    HighlightObject(hitObject);
                }

                // 🔥 FIXED: Use New Input System syntax
                // Was: if (Input.GetKeyDown(KeyCode.E))
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    highlightable.Interact();
                }
            }
            else
            {
                RemoveHighlight();
            }
        }
        else
        {
            RemoveHighlight();
        }
    }

    // ... (Keep the rest of your HighlightObject, RemoveHighlight, and CreateHighlightMaterial methods exactly the same) ...

    void HighlightObject(GameObject obj)
    {
        currentHighlightedObject = obj;
        currentRenderer = obj.GetComponent<Renderer>();

        if (currentRenderer == null) return;

        originalMaterials = currentRenderer.materials;
        highlightMaterials = new Material[originalMaterials.Length];

        for (int i = 0; i < originalMaterials.Length; i++)
            highlightMaterials[i] = highlightMaterial;

        currentRenderer.materials = highlightMaterials;

        HighlightableObject highlightable = obj.GetComponent<HighlightableObject>();
        if (highlightable != null) highlightable.OnHighlightEnter();
    }

    void RemoveHighlight()
    {
        if (currentHighlightedObject != null && currentRenderer != null)
        {
            currentRenderer.materials = originalMaterials;
            HighlightableObject highlightable = currentHighlightedObject.GetComponent<HighlightableObject>();
            if (highlightable != null) highlightable.OnHighlightExit();

            currentHighlightedObject = null;
            currentRenderer = null;
        }
    }

    Material CreateHighlightMaterial()
    {
        Material mat;
        if (Shader.Find("Universal Render Pipeline/Lit") != null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", highlightColor);
            mat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            mat.EnableKeyword("_EMISSION");
        }
        else
        {
            mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", highlightColor);
            mat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.5f);
        return mat;
    }
}