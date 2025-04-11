using UnityEngine;
using System.Collections;
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
    public float startingY = 490;
    public Vector3 outerSize = new Vector3(30f, 70f, 30f);
    public Vector3 outerCenter = new Vector3(0f, 0f, 0f);

    public float scale = 5f;

    // inner bounding box for cloud region that must be excluded (clouds only)
    public Vector3 innerCenter = new Vector3(0f, 0f, 0f);

    // keep track of spawned clouds
    private List<Cloud> cloudCollection = new List<Cloud>();

    void Start()
    {
        outerCenter = new Vector3(0f, startingY, 0f);
        innerCenter = new Vector3(0f, startingY, 0f);
        // spawn initial clouds
        StartCoroutine(CloudSpawnLoop());
    }

    IEnumerator CloudSpawnLoop()
    {
        // Then, continuously spawn clouds with delays
        for (int i = 0; i < initialCloudCount; i++)
        {
            float delay = Random.Range(1f, 5f); // tweak this as you like
            yield return new WaitForSeconds(delay);
            SpawnCloud();
        }
    }

    IEnumerator DelayedSpawnCloud(float delaySeconds)
    {
        // Debug.Log($"delaySeconds: {delaySeconds}");
        yield return new WaitForSeconds(delaySeconds);
        // Debug.Log($"waited for delaySeconds: {delaySeconds}");
        SpawnCloud();
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
                    // Debug.Log("deleting clouds and replacing");
                    Destroy(cloud.gameObject);
                    cloudCollection.RemoveAt(i);

                    // once destroyed, spawn a new cloud to replace it
                    float delaySeconds = Random.Range(0f, 10f);
                    StartCoroutine(DelayedSpawnCloud(delaySeconds));
                    // Debug.Log("replaced clouds");
                }
            }
        }
    }

    public enum Wall
    {
        Left,
        Right,
        Front,
        Back,
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
        CloudLightTracker tracker = cloudGO.AddComponent<CloudLightTracker>();

        // get a random position in the "outer" box that is NOT inside the "inner" box
        Vector3 spawnPos = GetRandomPositionExcludingInner();
        newCloud.transform.position = spawnPos;

        // determine which wall the cloud is closest to
        Wall cloudWall = DetermineWall(spawnPos);
        newCloud.transform.rotation = GetRotationForWall(cloudWall);
        Debug.Log($"Cloud is on wall: {cloudWall}");

        // store wall information
        newCloud.wallSide = cloudWall;

        // parent the cloud to this system for tidiness
        newCloud.transform.SetParent(this.transform);

        // initialize the cloud (assign materials, random movement, etc.)
        newCloud.Initialize(spawnPos, scale);

        // keep track of it
        cloudCollection.Add(newCloud);
    }

    private Wall DetermineWall(Vector3 position)
    {
        Vector3 halfOuter = outerSize / 2f;
        
        // calculate distance to each wall
        float distToLeft = Mathf.Abs(position.x - (outerCenter.x - halfOuter.x));
        float distToRight = Mathf.Abs(position.x - (outerCenter.x + halfOuter.x));
        float distToBack = Mathf.Abs(position.z - (outerCenter.z - halfOuter.z));
        float distToFront = Mathf.Abs(position.z - (outerCenter.z + halfOuter.z));

        // find nearest wall by comparing distances
        float minDist = Mathf.Min(distToLeft, distToRight, distToBack, distToFront);

        // return wall that matches the minimum distance
        if (minDist == distToLeft) return Wall.Left;
        else if (minDist == distToRight) return Wall.Right;
        else if (minDist == distToBack) return Wall.Back;
        return Wall.Front;
    }

    private Quaternion GetRotationForWall(Wall wall)
    {
        // set rotation so +z faces inward from the wall
        switch (wall)
        {
            case Wall.Left:
                return Quaternion.Euler(0, 90, 0);  // Rotate +z to face right
            case Wall.Right:
                return Quaternion.Euler(0, -90, 0); // Rotate +z to face left
            case Wall.Front:
                return Quaternion.Euler(0, 180, 0); // Rotate +z to face backward
            case Wall.Back:
                return Quaternion.Euler(0, 0, 0);   // Leave +z facing forward
            default:
                return Quaternion.identity;
        }
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

            if (!insideInner(candidate) && !insideCorner(candidate, 2.0f))
            {
                return candidate; // valid spawn point
            }
        }
    }

    /// Returns a random position in the outer bounding box, excluding any point in the inner bounding box.
    /// If random point falls inside the inner box, keep trying until it’s outside.

    private bool insideInner(Vector3 position) {
        // Debug.Log("inside Inner");
        Vector3 innerSize = eyepoolCube.GetEyepoolCubeSize();
        Vector3 halfInner = innerSize / 2f;
        // Debug.Log($"Inner Center: {innerCenter}");
        Vector3 minInner = innerCenter - halfInner;
        Vector3 maxInner = innerCenter + halfInner;
        // Debug.Log($"Inner Min:{minInner}, Max:{maxInner}");
        // Check if candidate is inside the inner bounding box
        bool insideInner =
            position.x >= (innerCenter.x - halfInner.x) &&
            position.x <= (innerCenter.x + halfInner.x) &&
            position.z >= (innerCenter.z - halfInner.z) &&
            position.z <= (innerCenter.z + halfInner.z);
        return insideInner;
    }

    private bool outsideOuter(Vector3 position) {
        // Debug.Log("outside Outer");
        Vector3 minBounds = outerCenter - outerSize / 2f;
        Vector3 maxBounds = outerCenter + outerSize / 2f;
        // // Debug.Log($"Outer Center: {outerCenter}");
        // Debug.Log($"Outer Min:{minBounds}, Max:{maxBounds}");

        bool outsideOuter = 
            position.x < minBounds.x || position.x > maxBounds.x ||
            position.y < minBounds.y || position.y > maxBounds.y ||
            position.z < minBounds.z || position.z > maxBounds.z;
        return outsideOuter;
    }

    private bool insideCorner(Vector3 position, float margin)
    {
        Vector3 halfOuter = outerSize / 2f;
        Vector3 outerMin = outerCenter - halfOuter;
        Vector3 outerMax = outerCenter + halfOuter;

        Vector3 innerSize = eyepoolCube.GetEyepoolCubeSize();
        Vector3 halfInner = innerSize / 2f;

        float cornerWidth = outerMax.x - halfInner.x;
        float cornerDepth = outerMax.z - halfInner.z;

        bool inLeft = position.x < outerMin.x + (outerSize.x - innerSize.x) / 2f + margin;
        bool inRight = position.x > outerMax.x - (outerSize.x - innerSize.x) / 2f - margin;
        bool inTop = position.z > outerMax.z - (outerSize.z - innerSize.z) / 2f - margin;
        bool inBottom = position.z < outerMin.z + (outerSize.z - innerSize.z) / 2f + margin;

        // Debug.Log($"left side bounds: {outerMin.x}, {outerSize.x}, {innerSize.x}, {(outerSize.x - innerSize.x) / 2f}, {outerMin.x + (outerSize.x - innerSize.x) / 2f}");
        // Debug.Log($"right side bounds: {outerMax.x}, {outerSize.x}, {innerSize.x}, {(outerSize.x - innerSize.x) / 2f}, {outerMax.x - (outerSize.x - innerSize.x) / 2f}"); 
        // Debug.Log($"top side bounds: {outerMax.z}, {outerSize.z}, {innerSize.z}, {(outerSize.z - innerSize.z) / 2f}, {outerMax.z - (outerSize.z - innerSize.z) / 2f}");
        // Debug.Log($"bottom side bounds: {outerMin.z}, {outerSize.z}, {innerSize.z}, {(outerSize.z - innerSize.z) / 2f}, {outerMin.z + (outerSize.z - innerSize.z) / 2f}");

        bool isCorner =
            (inLeft && inTop) ||  // Top-left
            (inRight && inTop) || // Top-right
            (inLeft && inBottom) || // Bottom-left
            (inRight && inBottom); // Bottom-right

        return isCorner;
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
