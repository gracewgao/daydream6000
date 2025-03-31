using UnityEngine;
using System.Collections.Generic;

public class CloudSystem : MonoBehaviour
{
    [Header("Cloud Spawning")]
    [Tooltip("Number of clouds to spawn initially")]
    public int initialCloudCount = 20;

    [Header("EYEPOOL scene cube")]
    [Tooltip("Inner Bounding box (clouds are excluded from)")]
    public EyepoolCubeGenerator eyepoolCube;

    // Outer bounding box (where we *can* place clouds).
    // If you truly want “infinite,” you can just make this huge 
    // or handle your wrap logic differently.
    public Vector3 outerSize = new Vector3(100f, 50f, 100f);
    public Vector3 outerCenter = Vector3.zero;

    // Inner bounding box that must be excluded
    public Vector3 innerCenter = Vector3.zero;

    // Keep track of spawned clouds
    private List<Cloud> cloudCollection = new List<Cloud>();

    void Start()
    {
        // Spawn initial clouds
        for (int i = 0; i < initialCloudCount; i++)
        {
            SpawnCloud();
        }
    }

    void Update()
    {
        // Move each cloud and wrap around bounding box
        foreach (Cloud cloud in cloudCollection)
        {
            if (cloud != null)
            {
                // Move the cloud
                cloud.transform.position += 
                    cloud.movementDirection.normalized * 
                    cloud.movementSpeed * 
                    Time.deltaTime;

                // Wrap if needed
                WrapAroundBoundingBox(cloud);
            }
        }

        // Optionally detect collisions or combine clouds, etc.
        DetectCollision();
    }

    /// Spawns a single cloud in the allowed region (outer minus inner).
    private void SpawnCloud() // TODO: I think there should be an intro animation object. 
    {
        // Make a basic placeholder Cloud game object
        GameObject cloudGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // Alternatively: Instantiate a prefab, if you have one
        cloudGO.name = "Cloud";

        // Add our Cloud script
        Cloud newCloud = cloudGO.AddComponent<Cloud>();

        // Get a random position in the "outer" box that is NOT inside the "inner" box
        Vector3 spawnPos = GetRandomPositionExcludingInner();
        newCloud.transform.position = spawnPos;

        // Parent the cloud to this system for tidiness
        newCloud.transform.SetParent(this.transform);

        // Initialize the cloud (assign materials, random movement, etc.)
        newCloud.Initialize();

        // Keep track of it
        cloudCollection.Add(newCloud);
    }

    /// Returns a random position in the outer bounding box, excluding any point in the inner bounding box.
    /// If random point falls inside the inner box, keep trying until it’s outside.
    private Vector3 GetRandomPositionExcludingInner()
    {
        Vector3 innerSize = eyepoolCube.GetEyepoolCubeSize();
        Vector3 halfOuter = outerSize / 2f;
        Vector3 halfInner = innerSize / 2f;

        while (true)
        {
            // Pick a random position within the outer bounding box
            float x = Random.Range(outerCenter.x - halfOuter.x, outerCenter.x + halfOuter.x);
            float y = Random.Range(outerCenter.y - halfOuter.y, outerCenter.y + halfOuter.y);
            float z = Random.Range(outerCenter.z - halfOuter.z, outerCenter.z + halfOuter.z);
            Vector3 candidate = new Vector3(x, y, z);

            // Check if candidate is inside the inner bounding box
            bool insideInner =
                candidate.x >= (innerCenter.x - halfInner.x) &&
                candidate.x <= (innerCenter.x + halfInner.x) &&
                candidate.y >= (innerCenter.y - halfInner.y) &&
                candidate.y <= (innerCenter.y + halfInner.y) &&
                candidate.z >= (innerCenter.z - halfInner.z) &&
                candidate.z <= (innerCenter.z + halfInner.z);

            if (!insideInner)
            {
                return candidate; // valid spawn point
            }
        }
    }

    /// If a cloud moves out of the outer bounding box, wrap it around to the other side.
    private void WrapAroundBoundingBox(Cloud cloud)
    {
        Vector3 minBounds = outerCenter - outerSize / 2f;
        Vector3 maxBounds = outerCenter + outerSize / 2f;
        Vector3 position = cloud.transform.position;

        if (position.x < minBounds.x) position.x = maxBounds.x;
        if (position.x > maxBounds.x) position.x = minBounds.x;
        if (position.y < minBounds.y) position.y = maxBounds.y;
        if (position.y > maxBounds.y) position.y = minBounds.y;
        if (position.z < minBounds.z) position.z = maxBounds.z;
        if (position.z > maxBounds.z) position.z = minBounds.z;

        cloud.transform.position = position;
    }

    // ---- Stubbed-out methods you can implement as needed ----
    public void DetectCollision()
    {
        // e.g. If you want to do bounding-box collisions among clouds
    }

    public void CombineClouds(Cloud a, Cloud b)
    {
        // e.g. If you want to merge clouds into bigger ones
    }
}
