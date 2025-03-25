using UnityEngine;
using System.Linq;

/*
Static class to simplify a "random bag" of clouds.
*/

public static class CloudMaterials
{
    private static Material[] allCloudMaterials;

    // loads all cloud materials from "Resources/Materials" folder.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeMaterials()
    {
        allCloudMaterials = Resources.LoadAll<Material>("Materials");
    }

    /// gets a random material from the cached array of materials.
    public static Material GetRandomMaterial()
    {
        if (allCloudMaterials == null || allCloudMaterials.Length == 0)
        {
            Debug.LogWarning("No materials found in Resources/Materials!");
            return null;
        }
        int idx = Random.Range(0, allCloudMaterials.Length);
        return allCloudMaterials[idx];
    }
}
