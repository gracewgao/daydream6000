using Augmenta;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System;

public class TrackedObjectData
{
    public Vector3 Position;
    public Vector3 SmoothedPosition;
    public float StationaryTime;
    public bool MaterialSet;

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
    public string materialFolderPath = "Materials";
    public string pairMaterialFolderPath = "PairMaterials";

    private Dictionary<int, TrackedObjectData> positions = new();
    private Dictionary<int, GameObject> objects = new();
    private List<Material> availableMaterials = new();
    private List<Material> pairMaterials = new();

    private float HEIGHT = 450f;
    private float LENGTH_SCALE = 24.6f / 8.84f;
    private float WIDTH_SCALE = 19.8f / 8.43f;
    private float START_SCALE = 0.2f;
    private float END_SCALE = 2.0f;
    private float END_SCALE_PAIR = 3.0f;
    private float SMOOTH_FACTOR = 0.2f;
    private float pairDistanceThreshold = 2.5f;

    void LoadMaterialsFromFolder()
    {
        Material[] materials = Resources.LoadAll<Material>(materialFolderPath);
        Material[] pairMats = Resources.LoadAll<Material>(pairMaterialFolderPath);

        if (materials.Length == 0) Debug.LogError("No materials found in " + materialFolderPath);
        if (pairMats.Length == 0) Debug.LogError("No materials found in " + pairMaterialFolderPath);

        availableMaterials = new List<Material>(materials);
        pairMaterials = new List<Material>(pairMats);
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

    void SpawnStationaryMesh(Vector3 position, bool usePairMaterial = false)
    {
        GameObject clone = new("StationaryMesh");
        clone.AddComponent<MeshFilter>().mesh = mesh;
        Renderer renderer = clone.AddComponent<MeshRenderer>();
        var materialList = usePairMaterial ? pairMaterials : availableMaterials;
        renderer.material = materialList[UnityEngine.Random.Range(0, materialList.Count)];

        if (clone.GetComponent<CloudLightTracker>() == null)
            clone.AddComponent<CloudLightTracker>();

        clone.transform.position = position;
        clone.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (usePairMaterial)
        {
            clone.transform.localScale = Vector3.one * END_SCALE_PAIR;
        }
        else
        {
            clone.transform.localScale = Vector3.one * END_SCALE;
        }
        
        clone.transform.SetParent(objectManager.transform);
        clone.AddComponent<TemporaryVisualEffect>();
    }

    void Start()
    {
        LoadMaterialsFromFolder();
    }

    void Update()
    {
        HashSet<int> grouped = new();
        var ids = new List<int>(positions.Keys);

        for (int i = 0; i < ids.Count; i++)
        {
            int id1 = ids[i];
            if (grouped.Contains(id1)) continue;

            List<int> group = new() { id1 };
            Vector3 groupCenter = positions[id1].SmoothedPosition;
            float groupTime = positions[id1].StationaryTime;

            for (int j = 0; j < ids.Count; j++)
            {
                if (i == j) continue;
                int id2 = ids[j];
                if (grouped.Contains(id2)) continue;

                Vector3 pos1 = positions[id1].SmoothedPosition;
                Vector3 pos2 = positions[id2].SmoothedPosition;

                if (ApproximatelyEqual(positions[id1].Position, pos1) &&
                    ApproximatelyEqual(positions[id2].Position, pos2) &&
                    Vector3.Distance(pos1, pos2) < pairDistanceThreshold)
                {
                    group.Add(id2);
                    groupCenter += pos2;
                    groupTime = Mathf.Min(groupTime, positions[id2].StationaryTime);
                }
            }

            if (group.Count > 1)
            {
                foreach (var gid in group)
                {
                    grouped.Add(gid);
                    positions[gid].StationaryTime += Time.deltaTime;
                }

                if (group.TrueForAll(id => positions[id].StationaryTime >= 3f && !positions[id].MaterialSet))
                {
                    Vector3 center = groupCenter / group.Count;
                    SpawnStationaryMesh(center, true);
                    foreach (var gid in group)
                    {
                        positions[gid].MaterialSet = true;
                    }
                }
            }
        }

        foreach (var pair in positions)
        {
            int id = pair.Key;
            TrackedObjectData data = pair.Value;

            data.SmoothedPosition += (data.Position - data.SmoothedPosition) * SMOOTH_FACTOR;

            if (grouped.Contains(id))
                continue;

            if (ApproximatelyEqual(data.Position, data.SmoothedPosition))
            {
                data.StationaryTime += Time.deltaTime;

                if (data.StationaryTime >= 5f && !data.MaterialSet)
                {
                    SpawnStationaryMesh(data.SmoothedPosition);
                    data.MaterialSet = true;
                }
            }
            else
            {
                data.StationaryTime = 0f;
                data.MaterialSet = false;
            }
        }
    }

    private void OnAugmentaObjectUpdate(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main) return;

        Vector3 currentPosition = new(
            augmentaObject.worldPosition3D.x * LENGTH_SCALE,
            augmentaObject.worldPosition3D.y + HEIGHT,
            augmentaObject.worldPosition3D.z * WIDTH_SCALE
        );

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

        if (positions.ContainsKey(augmentaObject.oid))
        {
            positions.Remove(augmentaObject.oid);
        }
    }
}
