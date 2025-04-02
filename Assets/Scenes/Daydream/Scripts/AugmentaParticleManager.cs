using Augmenta;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class AugmentaParticleManager : MonoBehaviour
{
    public AugmentaManager augmentaManager; // Reference to the Augmenta Manager
    public GameObject particlePrefab;       // Prefab for the particle system
    public GameObject particleManager;      // Parent object for the particle systems
    public Light sun;                       // Reference to the sun/directional light

    [Tooltip("Gradient for the sun color.")]
    public Gradient sunColorGradient;

    private Dictionary<int, GameObject> particleSystems = new Dictionary<int, GameObject>();
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

    // Gradient CreateSunGradient()
    // {
    //     Gradient gradient = new Gradient();

    //     // Set color keys (time must be 0 to 1)
    //     GradientColorKey[] colorKeys = new GradientColorKey[]
    //     {
    //         new GradientColorKey(new Color(0.622f,0.3469f,0.1850f), 0f),
    //         new GradientColorKey(new Color(0.7725f, 0.5529f, 0.3882f), 0.04f),
    //         new GradientColorKey(new Color(0.7529f, 0.6392f, 0.4901f), 0.1f),
    //         new GradientColorKey(new Color(0.7412f, 0.4901f, 0.3058f), 0.2f),
    //         new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 0.4f),
    //         new GradientColorKey(new Color(0.76f, 0.76f, 0.76f), 1.0f),
    //     };

    //     // Set alpha keys (usually just fade from 1 to 1 for full opacity)
    //     GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
    //     {
    //         new GradientAlphaKey(0.5f, 0.0f),
    //         new GradientAlphaKey(1.0f, 0.05f),
    //         new GradientAlphaKey(1.0f, 0.95f),
    //         new GradientAlphaKey(0.5f, 1.5f)
    //     };

    //     gradient.SetKeys(colorKeys, alphaKeys);

    //     return gradient;
    // }

    private void Update()
    {
        if (sun == null) return;

        float sunAngle = sun.transform.rotation.eulerAngles.x;
        float t = Mathf.InverseLerp(-2f, 182f, sunAngle);

        // mirror gradient bc u can only have 8 keys max
        float mirroredT = t > 0.5f ? 1f - t : t;
        Color tint = sunColorGradient.Evaluate(mirroredT * 2f);

        foreach (var psObj in particleSystems.Values)
        {
            var ps = psObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = tint;
            }
        }
    }

    private void OnAugmentaObjectUpdate(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main)
            return;

        Vector3 currentPosition = new Vector3(
            augmentaObject.worldPosition3D.x * LENGTH_SCALE,
            augmentaObject.worldPosition2D.y + HEIGHT + 0.5f,
            augmentaObject.worldPosition2D.z * WIDTH_SCALE
        );

        if (!particleSystems.ContainsKey(augmentaObject.oid))
        {
            GameObject newParticleSystem = Instantiate(particlePrefab, particleManager.transform);
            particleSystems.Add(augmentaObject.oid, newParticleSystem);
        }

        GameObject particleSystem = particleSystems[augmentaObject.oid];
        particleSystem.transform.position = currentPosition;
    }

    private void OnAugmentaObjectLeave(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main)
            return;

        if (particleSystems.ContainsKey(augmentaObject.oid))
        {
            GameObject particleSystem = particleSystems[augmentaObject.oid];
            Destroy(particleSystem);
            particleSystems.Remove(augmentaObject.oid);
        }
    }
}