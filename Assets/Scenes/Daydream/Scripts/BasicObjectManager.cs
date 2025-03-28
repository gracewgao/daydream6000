using Augmenta;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System;

public class TrackedObjectData
{
    public Vector3 Position;            // raw position
    public Vector3 SmoothedPosition;    // smoothed position
    public float StationaryTime;        // how long it's been still
    public bool MaterialSet;            // whether material has already been set

    public TrackedObjectData(Vector3 position, float stationaryTime = 0f, bool materialSet = false)
    {
        Position = position;
        SmoothedPosition = position;
        StationaryTime = stationaryTime;
        MaterialSet = materialSet;
    }
}

public class BasicObjectManager : MonoBehaviour
{
    public AugmentaManager augmentaManager;
    public GameObject objectManager;
    public Mesh mesh;
    public Material objectMaterial;

    private Dictionary<int, TrackedObjectData> positions = new Dictionary<int, TrackedObjectData>();
    private Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();

    public string materialFolderPath = "Materials";
    private List<Material> availableMaterials = new List<Material>();

    private float HEIGHT = 450f;
    private float LENGTH_SCALE = 24.6f / 8.84f;
    private float WIDTH_SCALE = 19.8f / 8.43f;
    private float START_SCALE = 0.2f;
    private float END_SCALE = 3.0f;
    private float SMOOTH_FACTOR = 0.2f; // 0 = no movement, 1 = no smoothing
    void LoadMaterialsFromFolder()
    {
        Material[] materials = Resources.LoadAll<Material>(materialFolderPath);
        if (materials.Length == 0)
        {
            Debug.LogError("No materials found in Assets/Resources/" + materialFolderPath);
            return;
        }

        availableMaterials = new List<Material>(materials);
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

    public static bool ApproximatelyEqual(Vector3 a, Vector3 b, float tolerance = 0.25f)
    {
        return Vector3.Distance(a, b) <= tolerance;
    }

    void SpawnStationaryMesh(Vector3 position)
    {
        GameObject clone = new GameObject("StationaryMesh");
        clone.AddComponent<MeshFilter>().mesh = mesh;
        Renderer renderer = clone.AddComponent<MeshRenderer>();
        renderer.material = availableMaterials[UnityEngine.Random.Range(0, availableMaterials.Count)];
        if (clone.GetComponent<CloudLightTracker>() == null)
        {
            clone.AddComponent<CloudLightTracker>();
        }

        clone.transform.position = position;
        clone.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        clone.transform.localScale = Vector3.one * END_SCALE;

        clone.transform.SetParent(objectManager.transform);

        // add scaling + rotatation
        clone.AddComponent<TemporaryVisualEffect>();
    }

    void Start()
    {
        LoadMaterialsFromFolder();
    }

    void Update()
    {
        foreach (var pair in positions)
        {
            int id = pair.Key;
            TrackedObjectData data = pair.Value;
            // GameObject obj = objects[id];

            // apply smoothing
            data.SmoothedPosition += (data.Position - data.SmoothedPosition) * SMOOTH_FACTOR;
            // obj.transform.position = data.SmoothedPosition;

            // obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            // float swayAmount = Mathf.Sin(Time.time * 0.5f) * 20f;
            // obj.transform.Rotate(swayAmount * Time.deltaTime, swayAmount * Time.deltaTime, swayAmount * Time.deltaTime);

            // check if stationary
            if (ApproximatelyEqual(data.Position, data.SmoothedPosition))
            {
                data.StationaryTime += Time.deltaTime;

                if (data.StationaryTime >= 1f && !data.MaterialSet)
                {
                    SpawnStationaryMesh(data.SmoothedPosition);
                    data.MaterialSet = true;
                }

                // if (data.StationaryTime > 0f && data.StationaryTime <= 3f)
                // {
                //     float t = data.StationaryTime / 3f;
                //     float scale = START_SCALE + (END_SCALE - START_SCALE) * t;
                //     obj.transform.localScale = Vector3.one * scale;
                // }
            }
            else
            {
                data.StationaryTime = 0f;
                data.MaterialSet = false;

                // obj.transform.localScale = Vector3.one * START_SCALE;

                // Renderer renderer = obj.GetComponent<Renderer>();
                // if (renderer != null)
                // {
                //     renderer.material = objectMaterial;     // sphere material
                //     // remove CloudLightTracker when mesh not visible
                //     var tracker = obj.GetComponent<CloudLightTracker>();
                //     if (tracker != null) Destroy(tracker);
                // }
            }
        }
    }

    private void OnAugmentaObjectUpdate(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main) return;

        Vector3 currentPosition = new Vector3(
            augmentaObject.worldPosition3D.x * LENGTH_SCALE,
            augmentaObject.worldPosition3D.y + HEIGHT,
            augmentaObject.worldPosition3D.z * WIDTH_SCALE
        );

        // if (!objects.ContainsKey(augmentaObject.oid))
        // {
        //     GameObject newObj = new GameObject("RandomObject_" + augmentaObject.oid);
        //     newObj.AddComponent<MeshFilter>().mesh = mesh;
        //     newObj.AddComponent<MeshRenderer>().material = objectMaterial;
        //     newObj.transform.SetParent(objectManager.transform);
        //     newObj.transform.localScale = Vector3.one * START_SCALE;
        //     objects[augmentaObject.oid] = newObj;
        // }

        if (!positions.ContainsKey(augmentaObject.oid))
        {
            positions[augmentaObject.oid] = new TrackedObjectData(currentPosition);
        }
        else
        {
            positions[augmentaObject.oid].Position = currentPosition;
        }
    }

    private void OnAugmentaObjectLeave(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main) return;

        // if (objects.ContainsKey(augmentaObject.oid))
        // {
        //     Destroy(objects[augmentaObject.oid]);
        //     objects.Remove(augmentaObject.oid);
        // }

        if (positions.ContainsKey(augmentaObject.oid))
        {
            positions.Remove(augmentaObject.oid);
        }
    }
}
