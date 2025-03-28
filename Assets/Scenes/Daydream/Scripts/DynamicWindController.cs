using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Augmenta;

public class DynamicWindController : MonoBehaviour
{
    public AugmentaManager augmentaManager;
    public Volume cloudVolume;

    [Tooltip("Multiplier to scale Augmenta speed to wind speed.")]
    public float windSpeedMultiplier = 50f;

    private VolumetricClouds volumetricClouds;
    private Dictionary<int, Vector3> lastPositions = new Dictionary<int, Vector3>();

    void Start()
    {
        if (cloudVolume == null || !cloudVolume.profile.TryGet(out volumetricClouds))
        {
            Debug.LogError("VolumetricClouds not found in the volume profile!");
        }
    }

    void Update()
    {
        if (volumetricClouds == null || augmentaManager == null) return;

        float totalSpeed = 0f;
        int count = 0;

        foreach (var kvp in augmentaManager.augmentaObjects)
        {
            var obj = kvp.Value;
            int id = obj.oid;

            Vector3 currentPosition = new Vector3(
                obj.worldPosition3D.x,
                obj.worldPosition3D.y,
                obj.worldPosition3D.z
            );

            if (lastPositions.ContainsKey(id))
            {
                float speed = Vector3.Distance(currentPosition, lastPositions[id]) / Time.deltaTime;
                totalSpeed += speed;
                count++;
                lastPositions[id] = currentPosition;
            }
            else
            {
                lastPositions[id] = currentPosition;
            }
        }

        float avgSpeed = count > 0 ? totalSpeed / count : 0f;
        float windSpeed = avgSpeed * windSpeedMultiplier;

        // set wind speed
        volumetricClouds.globalWindSpeed.overrideState = true;
        WindParameter.WindParamaterValue windValue = new WindParameter.WindParamaterValue
        {
            customValue = windSpeed,
            mode = WindParameter.WindOverrideMode.Custom
        };
        volumetricClouds.globalWindSpeed.value = windValue;
    }
}
