using Augmenta;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System;

public class TrackedObjectData
{
    public Vector3 Position;        // last known position
    public float StationaryTime;    // how long it's been still
    public bool MaterialSet;        // whether material has alreday been set

    public TrackedObjectData(Vector3 position, float stationaryTime = 0f, bool materialSet = false)
    {
        Position = position;
        StationaryTime = stationaryTime;
        MaterialSet = materialSet;
    }
}

public class BasicObjectManager : MonoBehaviour
{
    public AugmentaManager augmentaManager; // augmentaManager reference
    public GameObject objectManager;        // parent for objects
    public Mesh mesh;
    public Material objectMaterial;

    private Dictionary<int, TrackedObjectData> positions = new Dictionary<int, TrackedObjectData>();
    private Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();

    public string materialFolderPath = "Materials";
    private List<Material> availableMeshes = new List<Material>();

    private float HEIGHT = 250f;
    private float LENGTH_SCALE = 24.6f / 8.84f;
    private float WIDTH_SCALE = 19.8f / 8.43f;

    void LoadMeshesFromFolder()
    {
        Material[] meshes = Resources.LoadAll<Material>(materialFolderPath);
        if (meshes.Length == 0)
        {
            Debug.LogError("no materials found in Assets/Resources/" + materialFolderPath);
            return;
        }

        availableMeshes = new List<Material>(meshes);
    }

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

    public static bool ApproximatelyEqual(Vector3 a, Vector3 b, float tolerance = 0.5f)
    {
        return Mathf.Abs(a.x - b.x) <= tolerance &&
               Mathf.Abs(a.y - b.y) <= tolerance &&
               Mathf.Abs(a.z - b.z) <= tolerance;
    }

    void Start()
    {
        LoadMeshesFromFolder();
    }

    void Update()
    {
        foreach (var item in objects)
        {
            GameObject obj = item.Value;

            obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            
            float swayAmount = Mathf.Sin(Time.time * 2f) * 20f; // 20 degrees sway at 2 Hz
            obj.transform.Rotate(swayAmount * Time.deltaTime, swayAmount * Time.deltaTime, swayAmount * Time.deltaTime);
        }

        List<int> keys = new List<int>(positions.Keys);
        foreach (int key in keys)
        {
            TrackedObjectData value = positions[key];
            if (ApproximatelyEqual(value.Position, objects[key].transform.position))
            {
                GameObject obj = objects[key];
                positions[key] = new TrackedObjectData(value.Position, value.StationaryTime + Time.deltaTime, value.MaterialSet);

                if (value.StationaryTime > 1f && !value.MaterialSet) // change mesh if standing more than 1 second
                {
                    Renderer renderer = objects[key].GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Assign a random material to the gameobject
                        Material randomMaterial = availableMeshes[UnityEngine.Random.Range(0, availableMeshes.Count)];
                        renderer.material = randomMaterial;

                        // Also assign the script to the gameobject
                        if (obj.GetComponent<CloudLightTracker>() == null)
                        {
                            obj.AddComponent<CloudLightTracker>();
                        }
                    }
                    positions[key] = new TrackedObjectData(value.Position, value.StationaryTime, true);    // flag to indicate mesh has already been generated
                }

                if (value.StationaryTime <= 3f)  // growing
                {
                    float t = value.StationaryTime / 3f;
                    float scale = Mathf.Lerp(0.8f, 1.5f, t);
                    obj.transform.localScale = new Vector3(scale, scale, scale);
                }
            }
            else
            {
                GameObject obj = objects[key];

                positions[key] = new TrackedObjectData(obj.transform.position, 0f, false); // reset timer

                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = objectMaterial;     // sphere material

                    // remove CloudLightTracker when mesh not visible
                    if (obj.GetComponent<CloudLightTracker>() != null)
                    {
                        CloudLightTracker cloudLightTracker = obj.GetComponent<CloudLightTracker>();
                        Destroy(cloudLightTracker);
                    }
                }

                obj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            }
        }
    }

    // called when an Augmenta object is updated or enters the scene
    private void OnAugmentaObjectUpdate(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main)
            return;

        Vector3 currentPosition = new Vector3(
            augmentaObject.worldPosition3D.x * LENGTH_SCALE,
            augmentaObject.worldPosition3D.y + HEIGHT,
            augmentaObject.worldPosition3D.z * WIDTH_SCALE
        );

        if (!objects.ContainsKey(augmentaObject.oid))
        {
            GameObject newObject = new GameObject("RandomObject_" + augmentaObject.oid);
            MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = mesh;

            if (objectMaterial != null)
            {
                meshRenderer.material = objectMaterial;
            }
            newObject.transform.SetParent(objectManager.transform);
            objects.Add(augmentaObject.oid, newObject);

        }

        if (!positions.ContainsKey(augmentaObject.oid))
        {
            positions.Add(augmentaObject.oid, new TrackedObjectData(currentPosition, 0f, false));
        }

        GameObject gameObject = objects[augmentaObject.oid];
        gameObject.transform.position = currentPosition;

    }

    // called when an Augmenta object leaves the scene
    private void OnAugmentaObjectLeave(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main)
            return;

        if (objects.ContainsKey(augmentaObject.oid))
        {
            GameObject gameObject = objects[augmentaObject.oid];
            Destroy(gameObject);
            objects.Remove(augmentaObject.oid);
        }

        if (positions.ContainsKey(augmentaObject.oid))
        {
            positions.Remove(augmentaObject.oid);
        }
    }
}