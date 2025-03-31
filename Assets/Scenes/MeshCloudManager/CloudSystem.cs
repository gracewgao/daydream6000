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

    // outer bounding box for cloud region
    public float startingY = 410f;
    public Vector3 outerSize = new Vector3(30f, 50f, 30f);
    public Vector3 outerCenter = new Vector3(0f, 410f, 0f);

    public float scale = 5f;

    // inner bounding box for cloud region that must be excluded (clouds only)
    public Vector3 innerCenter = new Vector3(0f, 410f, 0f);

    // keep track of spawned clouds
    private List<Cloud> cloudCollection = new List<Cloud>();

    void Start()
    {
        outerCenter = new Vector3(0f, startingY, 0f);
        innerCenter = new Vector3(0f, startingY, 0f);
        // spawn initial clouds
        for (int i = 0; i < initialCloudCount; i++)
        {
            SpawnCloud();
        }
    }

    void Update()
    {
        // move each cloud
        for (int i = cloudCollection.Count - 1; i >= 0; i--)
        {
            Cloud cloud = cloudCollection[i];
            if (cloud != null)
            {
                cloud.UpdateCloud();
                
                // check if the cloud is inside the inner box or outside outer bounds.
                // if so, we remove it (and spawn a new one if we like).
                Vector3 position = cloud.transform.position;
                // WrapAroundBoundingBox(cloud);
                if (insideInner(position) || outsideOuter(position))
                {
                    Destroy(cloud.gameObject);
                    cloudCollection.RemoveAt(i);

                    // once destroyed, spawn a new cloud to replace it
                    SpawnCloud();
                }
            }
        }
    }

    /// spawns a single cloud in the allowed region (outer minus inner).
    private void SpawnCloud() // TODO: I think there should be an intro animation (e.g. "puff" as cloud emerges). 
    {
        // Debug.Log("spawned new cloud");
        // make a basic placeholder Cloud gameobject
        GameObject cloudGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloudGO.transform.localScale = Vector3.one * scale; // scale up by 5x
        cloudGO.name = "Cloud";

        // add our Cloud script
        Cloud newCloud = cloudGO.AddComponent<Cloud>();

        // get a random position in the "outer" box that is NOT inside the "inner" box
        Vector3 spawnPos = GetRandomPositionExcludingInner();
        newCloud.transform.position = spawnPos;

        // parent the cloud to this system for tidiness
        newCloud.transform.SetParent(this.transform);

        // initialize the cloud (assign materials, random movement, etc.)
        newCloud.Initialize(spawnPos);

        // keep track of it
        cloudCollection.Add(newCloud);
    }

    private Vector3 GetRandomPositionExcludingInner()
    {
        Vector3 halfOuter = outerSize / 2f;

        while (true)
        {
            // pick a random position within the outer bounding box
            float x = Random.Range(outerCenter.x - halfOuter.x, outerCenter.x + halfOuter.x);
            float y = Random.Range(outerCenter.y - halfOuter.y, outerCenter.y + halfOuter.y);
            float z = Random.Range(outerCenter.z - halfOuter.z, outerCenter.z + halfOuter.z);
            Vector3 candidate = new Vector3(x, startingY, z);

            if (!insideInner(candidate))
            {
                return candidate; // valid spawn point
            }
        }
    }

    /// Returns a random position in the outer bounding box, excluding any point in the inner bounding box.
    /// If random point falls inside the inner box, keep trying until it’s outside.

    private bool insideInner(Vector3 position) {
        Vector3 innerSize = eyepoolCube.GetEyepoolCubeSize();
        Vector3 halfInner = innerSize / 2f;
        Debug.Log($"Inner Center: {innerCenter}");
        Vector3 minInner = innerCenter - halfInner;
        Vector3 maxInner = innerCenter + halfInner;
        Debug.Log($"Inner Min:{minInner}, Max:{maxInner}");
        // Check if candidate is inside the inner bounding box
        bool insideInner =
            position.x >= (innerCenter.x - halfInner.x) &&
            position.x <= (innerCenter.x + halfInner.x) &&
            position.z >= (innerCenter.z - halfInner.z) &&
            position.z <= (innerCenter.z + halfInner.z);
        return insideInner;
    }

    private bool outsideOuter(Vector3 position) {
        Vector3 minBounds = outerCenter - outerSize / 2f;
        Vector3 maxBounds = outerCenter + outerSize / 2f;
        Debug.Log($"Outer Center: {outerCenter}");
        Debug.Log($"Outer Min:{minBounds}, Max:{maxBounds}");

        bool outsideOuter = 
            position.x < minBounds.x || position.x > maxBounds.x ||
            position.y < minBounds.y || position.y > maxBounds.y ||
            position.z < minBounds.z || position.z > maxBounds.z;
        return outsideOuter;
    }

    /// If a cloud moves out of the outer bounding box, wrap it around to the other side.
    // The cloud dies under any of the following conditions: 
    // 1. moves higher y than the FOV
    // 2. moves lower y than the FOV
    // 3. moves inside the bounding box
    private void WrapAroundBoundingBox(Cloud cloud) // TODO: change this logic to spawn a new cloud
    {
        Vector3 position = cloud.transform.position;
        if (insideInner(position) || outsideOuter(position)) {
            Destroy(cloud.gameObject);
            cloudCollection.Remove(cloud); 
            SpawnCloud();
            return;
        }
    }

    // Likely won't need collision logic, but included anyways
    public void DetectCollision()
    {
        // e.g. If we want to do bounding-box collisions among clouds
    }

    public void CombineClouds(Cloud a, Cloud b)
    {
        // e.g. If we want to merge clouds into bigger ones
    }
}
