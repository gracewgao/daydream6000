using Augmenta;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class TrackedVFXObjectData
{
    public Vector3 Position;            // raw position
    public Vector3 SmoothedPosition;    // smoothed position
    
    public TrackedVFXObjectData(Vector3 position)
    {
        Position = position;
        SmoothedPosition = position;
    }
}

public class AugmentaVFXManager : MonoBehaviour
{
    public AugmentaManager augmentaManager; // Reference to the Augmenta Manager
    public GameObject vfxPrefab;            // Prefab containing the VFX Graph
    public GameObject vfxManager;           // Parent object for the VFX instances
    public Light directionalLight;          // Reference to main directional light for sunset effect

    private Dictionary<int, GameObject> vfxInstances = new Dictionary<int, GameObject>();
    private Dictionary<int, VisualEffect> vfxComponents = new Dictionary<int, VisualEffect>();
    private Dictionary<int, TrackedVFXObjectData> trackedObjects = new Dictionary<int, TrackedVFXObjectData>();
    private Dictionary<int, CloudLightTracker> cloudTrackers = new Dictionary<int, CloudLightTracker>();

    private float HEIGHT = 450f;
    private float LENGTH_SCALE = 24.6f / 8.84f;
    private float WIDTH_SCALE = 19.8f / 8.43f;
    private float SMOOTH_FACTOR = 0.2f; // 0 = no movement, 1 = no smoothing

    private void OnEnable()
    {
        augmentaManager.augmentaObjectUpdate += OnAugmentaObjectUpdate;
        augmentaManager.augmentaObjectLeave += OnAugmentaObjectLeave;
        
        // Find directional light if not set
        if (directionalLight == null)
        {
            directionalLight = FindFirstObjectByType<Light>(FindObjectsInactive.Exclude);
            if (directionalLight == null || directionalLight.type != LightType.Directional)
            {
                Debug.LogWarning("No directional light found for cloud lighting effects");
            }
        }
    }

    private void OnDisable()
    {
        augmentaManager.augmentaObjectUpdate -= OnAugmentaObjectUpdate;
        augmentaManager.augmentaObjectLeave -= OnAugmentaObjectLeave;
    }

    public static bool ApproximatelyEqual(Vector3 a, Vector3 b, float tolerance = 0.25f)
    {
        return Vector3.Distance(a, b) <= tolerance;
    }

    void Update()
    {
        foreach (var pair in trackedObjects)
        {
            int id = pair.Key;
            TrackedVFXObjectData data = pair.Value;
            
            if (!vfxInstances.ContainsKey(id) || !vfxComponents.ContainsKey(id))
                continue;
                
            VisualEffect vfx = vfxComponents[id];

            // Apply smoothing
            data.SmoothedPosition += (data.Position - data.SmoothedPosition) * SMOOTH_FACTOR;
            
            // Check if stationary
            bool isMoving = !ApproximatelyEqual(data.Position, data.SmoothedPosition);
            vfx.SetBool("EnableSpawning", isMoving);
            
            // Set spawn position for new particles
            vfx.SetVector3("SpawnPosition", data.SmoothedPosition);
        }
    }

    // Called when an object is updated or enters the scene
    private void OnAugmentaObjectUpdate(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main)
            return;

        Vector3 currentPosition = new Vector3(
            augmentaObject.worldPosition3D.x * LENGTH_SCALE, 
            augmentaObject.worldPosition3D.y + HEIGHT + 0.5f,
            augmentaObject.worldPosition3D.z * WIDTH_SCALE
        );

        // Create new VFX instance if it doesn't exist
        if (!vfxInstances.ContainsKey(augmentaObject.oid))
        {
            GameObject newVfxInstance = Instantiate(vfxPrefab, vfxManager.transform);
            VisualEffect vfxComponent = newVfxInstance.GetComponent<VisualEffect>();
            
            if (vfxComponent == null)
            {
                Debug.LogError("VFX Prefab does not contain a VisualEffect component!");
                return;
            }
            
            // Add CloudLightTracker component
            CloudLightTracker cloudTracker = newVfxInstance.AddComponent<CloudLightTracker>();
            cloudTracker.directionalLight = directionalLight;
            cloudTracker.vfxGraph = vfxComponent;
            
            vfxInstances.Add(augmentaObject.oid, newVfxInstance);
            vfxComponents.Add(augmentaObject.oid, vfxComponent);
            trackedObjects.Add(augmentaObject.oid, new TrackedVFXObjectData(currentPosition));
            
            // Initialize VFX
            vfxComponent.SetBool("EnableSpawning", true);
            vfxComponent.SetVector3("SpawnPosition", currentPosition);
        }
        else
        {
            // Just update the raw position, smoothing is handled in Update()
            trackedObjects[augmentaObject.oid].Position = currentPosition;
        }

        cloudTrackers[augmentaObject.oid].UpdateLightDirection();
    }

    // Called when an object leaves the scene
    private void OnAugmentaObjectLeave(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main)
            return;

        // Remove the VFX instance associated with this object
        if (vfxInstances.ContainsKey(augmentaObject.oid))
        {
            GameObject vfxInstance = vfxInstances[augmentaObject.oid];
            Destroy(vfxInstance);
            vfxInstances.Remove(augmentaObject.oid);
            vfxComponents.Remove(augmentaObject.oid);
            trackedObjects.Remove(augmentaObject.oid);
            cloudTrackers.Remove(augmentaObject.oid);
        }
    }
    
    // Optional: Update all trackers if light changes
    public void UpdateAllLightTrackers()
    {
        foreach (var tracker in cloudTrackers.Values)
        {
            if (tracker != null)
            {
                tracker.UpdateLightDirection();
            }
        }
    }
}
