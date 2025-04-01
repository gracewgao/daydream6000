using Augmenta;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class AugmentaVFXManager : MonoBehaviour
{
    public AugmentaManager augmentaManager; // Reference to the Augmenta Manager
    public GameObject vfxPrefab;            // Prefab containing the VFX Graph
    public GameObject vfxManager;           // Parent object for the VFX instances

    private Dictionary<int, GameObject> vfxInstances = new Dictionary<int, GameObject>();
    private Dictionary<int, VisualEffect> vfxComponents = new Dictionary<int, VisualEffect>();
    private Dictionary<int, Vector3> previousPositions = new Dictionary<int, Vector3>();

    private float HEIGHT = 450f;
    private float LENGTH_SCALE = 24.6f / 8.84f;
    private float WIDTH_SCALE = 19.8f / 8.43f;

    private void OnEnable()
    {
        augmentaManager.augmentaObjectUpdate += OnAugmentaObjectUpdate;
        augmentaManager.augmentaObjectLeave += OnAugmentaObjectLeave;
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

        if (!vfxInstances.ContainsKey(augmentaObject.oid))
        {
            // Create new VFX instance
            GameObject newVfxInstance = Instantiate(vfxPrefab, vfxManager.transform);
            VisualEffect vfxComponent = newVfxInstance.GetComponent<VisualEffect>();
            
            if (vfxComponent == null)
            {
                Debug.LogError("VFX Prefab does not contain a VisualEffect component!");
                return;
            }
            
            vfxInstances.Add(augmentaObject.oid, newVfxInstance);
            vfxComponents.Add(augmentaObject.oid, vfxComponent);
            previousPositions.Add(augmentaObject.oid, currentPosition);
        }
        
        GameObject vfxInstance = vfxInstances[augmentaObject.oid];
        VisualEffect vfx = vfxComponents[augmentaObject.oid];

        // check if particles should spawn
        Vector3 previousPosition = previousPositions[augmentaObject.oid];
        bool isMoving = !ApproximatelyEqual(currentPosition, previousPosition);
        vfx.SetBool("EnableSpawning", isMoving);
        
        // set spawn position for new particles
        vfx.SetVector3("SpawnPosition", currentPosition);

        previousPositions[augmentaObject.oid] = currentPosition;
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
            previousPositions.Remove(augmentaObject.oid);
        }
    }
}
