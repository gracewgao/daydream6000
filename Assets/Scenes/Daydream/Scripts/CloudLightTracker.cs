using UnityEngine;

[ExecuteInEditMode]
public class CloudLightTracker : MonoBehaviour
{
    private Renderer objectRenderer;
    public Light directionalLight;
    
    private Vector3 lastLightDirection;
    private Transform lastParent;
    private Quaternion lastRotation;
    
    void OnEnable()
    {
        objectRenderer = GetComponent<Renderer>();
        
        if (directionalLight == null)
            directionalLight = FindFirstObjectByType<Light>();
            
        UpdateLightDirection();
    }
    
    void LateUpdate()
    {
        if (directionalLight == null || objectRenderer == null)
            return;
            
        // only update if something relevant has changed
        if (lastLightDirection != directionalLight.transform.forward || 
            transform.parent != lastParent || 
            transform.rotation != lastRotation)
        {
            UpdateLightDirection();
        }
    }
    
    void UpdateLightDirection()
    {
        if (directionalLight != null && objectRenderer != null && objectRenderer.sharedMaterial != null)
        {
            // Convert light direction to object space for shadows
            Vector3 objectSpaceSunDir = transform.InverseTransformDirection(-directionalLight.transform.forward);
            
            // Get the global sun direction for consistent sunset coloring
            Vector3 globalSunDir = -directionalLight.transform.forward;
            
            // Update shader properties
            objectRenderer.sharedMaterial.SetVector("_SunDirection", objectSpaceSunDir);
            objectRenderer.sharedMaterial.SetVector("_GlobalSunDirection", globalSunDir);
            
            // Pass the light color to the shader (without intensity)
            Color lightColor = directionalLight.color;
            objectRenderer.sharedMaterial.SetColor("_LightColor", lightColor);
            
            // Cache values
            lastLightDirection = directionalLight.transform.forward;
            lastParent = transform.parent;
            lastRotation = transform.rotation;
        }
    }
}

