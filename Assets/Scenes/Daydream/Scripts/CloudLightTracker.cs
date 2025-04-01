using UnityEngine;
using UnityEngine.VFX; // Add this for VisualEffect

[ExecuteInEditMode]
public class CloudLightTracker : MonoBehaviour
{
    private Renderer objectRenderer;
    public Light directionalLight;
    
    // Add reference to your VFX Graph
    public VisualEffect vfxGraph;
    
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
    
    public void UpdateLightDirection()
    {
        if (directionalLight != null)
        {
            if (objectRenderer != null && objectRenderer.sharedMaterial != null)
            {
                // convert light direction to object space for shader
                Vector3 lightDir = transform.InverseTransformDirection(-directionalLight.transform.forward);
                
                // update shader light direction
                objectRenderer.sharedMaterial.SetVector("_SunDirection", lightDir);
                
                // Pass the light color to the shader (without intensity)
                Color lightColor = directionalLight.color;
                objectRenderer.sharedMaterial.SetColor("_LightColor", lightColor);
            }
            
            // Update VFX Graph if available
            if (vfxGraph != null)
            {
                // For VFX Graph, we typically use world space direction
                Vector3 vfxLightDir = -directionalLight.transform.forward;
                vfxGraph.SetVector3("SunDirection", vfxLightDir);
            }
            
            // Cache values
            lastLightDirection = directionalLight.transform.forward;
            lastParent = transform.parent;
            lastRotation = transform.rotation;
        }
    }
}
