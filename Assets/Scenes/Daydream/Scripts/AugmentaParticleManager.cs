using Augmenta;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class AugmentaParticleManager : MonoBehaviour
{
    public AugmentaManager augmentaManager;
    public GameObject particlePrefab;
    public GameObject particleManager;
    public Light sun;
    [Tooltip("Gradient for the sun color.")]
    public Gradient sunColorGradient;

    private Dictionary<int, GameObject> particleSystems = new();
    private Dictionary<int, Vector3> previousPositions = new();
    private HashSet<(int, int)> activePairs = new();

    private float HEIGHT = 450f;
    private float LENGTH_SCALE = 24.6f / 8.84f;
    private float WIDTH_SCALE = 19.8f / 8.43f;

    private float pairThreshold = 2.5f; // distance below which particles orbit
    private float orbitRadius = 1.0f;
    private float orbitSpeed = 1.0f;

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

    private void Update()
    {
        if (sun == null) return;

        float sunAngle = sun.transform.rotation.eulerAngles.x;
        float t = Mathf.InverseLerp(-2f, 182f, sunAngle);
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

    private void OnAugmentaObjectUpdate(AugmentaObject augmentaObject, AugmentaDataType dataType)
    {
        if (dataType != AugmentaDataType.Main)
            return;

        Vector3 currentPosition = new(
            augmentaObject.worldPosition3D.x * LENGTH_SCALE,
            augmentaObject.worldPosition2D.y + HEIGHT + 0.5f,
            augmentaObject.worldPosition2D.z * WIDTH_SCALE
        );

        previousPositions[augmentaObject.oid] = currentPosition;

        if (!particleSystems.ContainsKey(augmentaObject.oid))
        {
            GameObject ps = Instantiate(particlePrefab, particleManager.transform);
            particleSystems.Add(augmentaObject.oid, ps);
        }

        HandlePairingLogic();
    }

    private void HandlePairingLogic()
    {
        List<int> oids = new(previousPositions.Keys);
        HashSet<int> paired = new();
        activePairs.Clear();

        for (int i = 0; i < oids.Count; i++)
        {
            for (int j = i + 1; j < oids.Count; j++)
            {
                int oid1 = oids[i];
                int oid2 = oids[j];
                Vector3 pos1 = previousPositions[oid1];
                Vector3 pos2 = previousPositions[oid2];
                float dist = Vector3.Distance(pos1, pos2);

                if (dist < pairThreshold)
                {
                    Vector3 midpoint = (pos1 + pos2) / 2;
                    float angle = Time.time * orbitSpeed;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;

                    if (particleSystems.TryGetValue(oid1, out var ps1) &&
                        particleSystems.TryGetValue(oid2, out var ps2))
                    {
                        ps1.transform.position = Vector3.Lerp(ps1.transform.position, midpoint + offset, Time.deltaTime * 4f);
                        ps2.transform.position = Vector3.Lerp(ps2.transform.position, midpoint - offset, Time.deltaTime * 4f);
                        paired.Add(oid1);
                        paired.Add(oid2);
                        activePairs.Add((oid1, oid2));
                    }
                }
            }
        }

        // Update non-paired particles normally
        foreach (var kvp in particleSystems)
        {
            if (!paired.Contains(kvp.Key))
            {
                Vector3 targetPos = previousPositions[kvp.Key];
                kvp.Value.transform.position = Vector3.Lerp(kvp.Value.transform.position, targetPos, Time.deltaTime * 4f);
            }
        }
    }

    private void OnAugmentaObjectLeave(AugmentaObject augmentaObject, AugmentaDataType dataType)
    {
        if (dataType != AugmentaDataType.Main)
            return;

        if (particleSystems.TryGetValue(augmentaObject.oid, out var ps))
        {
            Destroy(ps);
            particleSystems.Remove(augmentaObject.oid);
            previousPositions.Remove(augmentaObject.oid);
        }
    }
}
