using UnityEngine;
using Augmenta;
using System.Collections;
using System.Collections.Generic;

public class CloudBridgeManager : MonoBehaviour
{
    public AugmentaManager augmentaManager;
    public GameObject orbitingParticlePrefab;
    public float detectionRadius = 1.5f;
    public Transform effectParent;
    public float orbitRadius = 1.5f;
    public float orbitSpeed = 1f;
    public float moveSpeed = 2f;
    public Light sun;
    public Gradient sunColorGradient;

    private GameObject particleA;
    private GameObject particleB;
    private GameObject bridgeEffect;
    private bool bridgeActive = false;
    private bool movingToB = true;
    private float angleA = 0f;
    private float angleB = Mathf.PI; // Start 180 degrees apart
    private Color currentTint = Color.white;
    private Vector3 centerA;
    private Vector3 centerB;

    void Start()
    {
        SpawnOrbitingParticles();
    }

    void SpawnOrbitingParticles()
    {
        centerA = new Vector3(-4f, 450f, 0f); // manually set center
        centerB = new Vector3(4f, 450f, 0f);  // manually set center

        particleA = Instantiate(orbitingParticlePrefab);
        particleB = Instantiate(orbitingParticlePrefab);

        if (effectParent != null)
        {
            particleA.transform.SetParent(effectParent);
            particleB.transform.SetParent(effectParent);
        }

        ApplyTintToParticle(particleA);
        ApplyTintToParticle(particleB);
    }


    void ApplyTintToParticle(GameObject particle)
    {
        var ps = particle?.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = currentTint;
        }
    }

    void Update()
    {
        if (sun != null)
        {
            float sunAngle = sun.transform.rotation.eulerAngles.x;
            float t = Mathf.InverseLerp(-2f, 182f, sunAngle);
            float mirroredT = t > 0.5f ? 1f - t : t;
            currentTint = sunColorGradient.Evaluate(mirroredT * 2f);
        }

        ApplyTintToParticle(particleA);
        ApplyTintToParticle(particleB);
        ApplyTintToParticle(bridgeEffect);

        if (particleA == null || particleB == null)
            return;

        // Animate circular motion
        angleA += orbitSpeed * Time.deltaTime;
        angleB += orbitSpeed * Time.deltaTime;

        float xA = Mathf.Cos(angleA) * orbitRadius;
        float zA = Mathf.Sin(angleA) * orbitRadius;
        float xB = Mathf.Cos(angleB) * orbitRadius;
        float zB = Mathf.Sin(angleB) * orbitRadius;

        particleA.transform.position = centerA + new Vector3(xA, 0, zA);
        particleB.transform.position = centerB + new Vector3(xB, 0, zB);

        // Check for occupancy
        bool aOccupied = false;
        bool bOccupied = false;

        foreach (var obj in augmentaManager.augmentaObjects)
        {
            Vector3 pos = new Vector3(
                obj.Value.worldPosition3D.x * 24.6f / 8.84f,
                450f,
                obj.Value.worldPosition3D.z * 19.8f / 8.43f
            );

            if (Vector3.Distance(pos, particleA.transform.position) < detectionRadius)
                aOccupied = true;

            if (Vector3.Distance(pos, particleB.transform.position) < detectionRadius)
                bOccupied = true;
        }

        if (aOccupied && bOccupied)
        {
            if (!bridgeActive)
            {
                bridgeEffect = Instantiate(orbitingParticlePrefab);
                bridgeEffect.transform.position = particleA.transform.position;
                if (effectParent != null)
                    bridgeEffect.transform.SetParent(effectParent);
                bridgeActive = true;
                movingToB = true;
                ApplyTintToParticle(bridgeEffect);
            }
            else if (bridgeEffect != null)
            {
                Vector3 target = movingToB ? particleB.transform.position : particleA.transform.position;
                bridgeEffect.transform.position = Vector3.MoveTowards(
                    bridgeEffect.transform.position,
                    target,
                    moveSpeed * Time.deltaTime
                );

                bridgeEffect.transform.LookAt(target);

                if (Vector3.Distance(bridgeEffect.transform.position, target) < 0.1f)
                {
                    movingToB = !movingToB;
                }
            }
        }
        else
        {
            if (bridgeEffect != null)
            {
                Destroy(bridgeEffect);
                bridgeActive = false;
            }
        }
    }
}
