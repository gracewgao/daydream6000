using Augmenta;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System;

public class BasicObjectManager : MonoBehaviour
{
    public AugmentaManager augmentaManager; // augmentaManager reference
    public GameObject objectManager;        // parent for objects
    public Mesh mesh;
    public Material objectMaterial;

    private Dictionary<int, Tuple<Vector3, float>> positions = new Dictionary<int, Tuple<Vector3, float>>();
    private Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();

    public string meshFolderPath = "Meshes";
    private List<Mesh> availableMeshes = new List<Mesh>();

    void LoadMeshesFromFolder()
    {
        Mesh[] meshes = Resources.LoadAll<Mesh>(meshFolderPath);
        if (meshes.Length == 0)
        {
            Debug.LogError("no meshes found in Assets/Resources/" + meshFolderPath);
            return;
        }

        availableMeshes = new List<Mesh>(meshes);
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

    public static bool ApproximatelyEqual(Vector3 a, Vector3 b, float tolerance = 0.1f)
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
            obj.transform.Rotate(10 * Time.deltaTime, 10 * Time.deltaTime, 10 * Time.deltaTime);
        }

        List<int> keys = new List<int>(positions.Keys);
        foreach (int key in keys)
        {
            Tuple<Vector3, float> value = positions[key];
            // todo: be more generous with changes
            // Debug.Log(key + " with value.Item1: " + value.Item1 + " and value.Item2: " + value.Item2 + " and object position is " + objects[key].transform.position);
            if (ApproximatelyEqual(value.Item1, objects[key].transform.position))
            {
                if (value.Item2 < 0)
                {
                    continue; // skip if mesh already updated
                }
                if (value.Item2 > 5f)
                {
                    MeshFilter meshFilter = objects[key].GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        Mesh randomMesh = availableMeshes[UnityEngine.Random.Range(0, availableMeshes.Count)];
                        meshFilter.mesh = randomMesh;
                    }
                    positions[key] = new Tuple<Vector3, float>(value.Item1, -1);    // flag to indicate mesh has already been generated
                }
                else
                {
                    positions[key] = new Tuple<Vector3, float>(value.Item1, value.Item2 + Time.deltaTime);
                }
            }
            else
            {
                positions[key] = new Tuple<Vector3, float>(objects[key].transform.position, 0f);
                MeshFilter meshFilter = objects[key].GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshFilter.mesh = mesh; // set to sphere when walking
                }
            }
        }
    }

    // called when an Augmenta object is updated or enters the scene
    private void OnAugmentaObjectUpdate(AugmentaObject augmentaObject, AugmentaDataType augmentaDataType)
    {
        if (augmentaDataType != AugmentaDataType.Main)
            return;

        Vector3 currentPosition = new Vector3(
            augmentaObject.worldPosition3D.x,
            augmentaObject.worldPosition3D.y - 2f,
            augmentaObject.worldPosition3D.z
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
            positions.Add(augmentaObject.oid, new Tuple<Vector3, float>(currentPosition, 0f));
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