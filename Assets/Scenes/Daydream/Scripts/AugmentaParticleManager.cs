using Augmenta;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class AugmentaParticleManager : MonoBehaviour
{
    public AugmentaManager augmentaManager; // Reference to the Augmenta Manager
    public GameObject particlePrefab;       // Prefab for the particle system
    public GameObject particleManager;      // Parent object for the particle systems

    private Dictionary<int, GameObject> particleSystems = new Dictionary<int, GameObject>();
    private Dictionary<int, Vector3> previousPositions = new Dictionary<int, Vector3>();


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

    // Called when an object is updated or enters the scene
    private void OnAugmentaObjectUpdate(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main)
            return;

        // Vector3 currentPosition = augmentaObject.worldPosition3D;
        Vector3 currentPosition = new Vector3(
            augmentaObject.worldPosition3D.x, 
            augmentaObject.worldPosition2D.y - 1f,
            augmentaObject.worldPosition2D.z
        );

        if (!particleSystems.ContainsKey(augmentaObject.oid))
        {
            GameObject newParticleSystem = Instantiate(particlePrefab, particleManager.transform);
            particleSystems.Add(augmentaObject.oid, newParticleSystem);
        }
        
        GameObject particleSystem = particleSystems[augmentaObject.oid];
        particleSystem.transform.position = currentPosition;
    }

    // Called when an object leaves the scene
    private void OnAugmentaObjectLeave(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main)
            return;

        // Remove the particle system associated with this object
        if (particleSystems.ContainsKey(augmentaObject.oid))
        {
            GameObject particleSystem = particleSystems[augmentaObject.oid];
            Destroy(particleSystem); // Destroy the particle system GameObject
            particleSystems.Remove(augmentaObject.oid);
        }
    }
}