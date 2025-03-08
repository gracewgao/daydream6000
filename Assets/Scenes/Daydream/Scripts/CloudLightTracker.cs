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
        if (lastLightDirection != directionalLight.transform.forward || transform.parent != lastParent || transform.rotation != lastRotation)
        {
            UpdateLightDirection();
        }
    }
    
    void UpdateLightDirection()
    {
        if (directionalLight != null && objectRenderer != null && objectRenderer.sharedMaterial != null)
        {
            // convert light direction to object space
            Vector3 lightDir = transform.InverseTransformDirection(-directionalLight.transform.forward);
            
            // update shader
            Debug.Log("Updating Sun direction property to (" + lightDir.x + ", " + lightDir.y + ", " + lightDir.z + ")");
            objectRenderer.sharedMaterial.SetVector("_SunDirection", lightDir);
            
            // cache values
            lastLightDirection = directionalLight.transform.forward;
            lastParent = transform.parent;
            lastRotation = transform.rotation;
        }
    }
}

