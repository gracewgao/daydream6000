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
    public float windSpeedMultiplier = 10f;

    [Tooltip("Smoothing factor (0 = very slow change, 1 = instant change). Recommended: 0.1")]
    [Range(0f, 1f)]
    public float smoothingAlpha = 0.1f;

    [Tooltip("Max allowed raw movement speed before ignoring the data (to reject teleports).")]
    public float maxMovementPerSecond = 10f;

    [Tooltip("Clamp final wind speed to this max value (to prevent clouds from going crazy).")]
    public float maxWindSpeed = 100f;

    private VolumetricClouds volumetricClouds;
    private Dictionary<int, Vector3> lastPositions = new Dictionary<int, Vector3>();
    private float timeSinceLastUpdate = 0f;

    private float smoothedWindSpeed = 0f;

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

        if (timeSinceLastUpdate < 1f)
        {
            timeSinceLastUpdate += Time.deltaTime;
            return;
        }
        else
        {
            timeSinceLastUpdate = 0f;
        }

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
                float distance = Vector3.Distance(currentPosition, lastPositions[id]);
                float speed = distance / Time.deltaTime;

                // Ignore extreme movement (e.g. teleportation)
                if (speed <= maxMovementPerSecond)
                {
                    totalSpeed += speed;
                    count++;
                }

                lastPositions[id] = currentPosition;
            }
            else
            {
                lastPositions[id] = currentPosition;
            }
        }

        float avgSpeed = count > 0 ? totalSpeed / count : 0f;

        // Apply multiplier and clamp raw speed
        float rawWindSpeed = Mathf.Min(avgSpeed * windSpeedMultiplier, maxWindSpeed);

        // Smooth using EMA
        smoothedWindSpeed = smoothingAlpha * rawWindSpeed + (1f - smoothingAlpha) * smoothedWindSpeed;

        // Debug.Log($"AvgSpeed: {avgSpeed:F2}, RawWind: {rawWindSpeed:F2}, SmoothedWind: {smoothedWindSpeed:F2}");

        // Set wind speed
        volumetricClouds.globalWindSpeed.overrideState = true;
        WindParameter.WindParamaterValue windValue = new WindParameter.WindParamaterValue
        {
            customValue = smoothedWindSpeed + 10, // Base wind offset
            mode = WindParameter.WindOverrideMode.Custom
        };
        volumetricClouds.globalWindSpeed.value = windValue;
    }
}
