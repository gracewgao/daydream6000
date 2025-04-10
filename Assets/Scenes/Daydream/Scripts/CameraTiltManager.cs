using UnityEngine;
using Augmenta;

public class CameraTiltManager : MonoBehaviour
{
    public AugmentaManager augmentaManager;
    public GameObject camera1;
    public GameObject camera2;
    public GameObject camera3;
    public GameObject camera4;

    [Tooltip("rotation at 0 people")]
    public float minRotation = 5f;

    [Tooltip("rotation at 10+ people")]
    public float maxRotation = -15f;

    [Tooltip("speed at which the tilt smooths toward the target")]
    public float smoothSpeed = 0.05f;

    private float currentXRotation = 5f;

    private void Update()
    {
        if (augmentaManager == null)
            return;

        int count = 0;
        foreach (var obj in augmentaManager.augmentaObjects)
        {
            count++;
        }

        float t = Mathf.InverseLerp(0f, 10f, count);
        float targetXRotation = Mathf.Lerp(minRotation, maxRotation, t);

        // Smoothly interpolate the rotation
        currentXRotation = Mathf.Lerp(currentXRotation, targetXRotation, Time.deltaTime * smoothSpeed);

        RotateX(camera1, currentXRotation);
        RotateX(camera2, currentXRotation);
        RotateX(camera3, currentXRotation);
        RotateX(camera4, currentXRotation);
    }

    private void RotateX(GameObject cam, float xRotation)
    {
        if (cam == null) return;
        Vector3 currentEuler = cam.transform.eulerAngles;
        currentEuler.x = xRotation;
        cam.transform.eulerAngles = currentEuler;
    }
}
