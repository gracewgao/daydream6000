using UnityEngine;
using System.Collections.Generic;

public class CameraSwitcher : MonoBehaviour
{
    public List<Camera> cameras;  // List to store cameras
    private int currentCameraIndex = 0;  // To keep track of the active camera

    void Start()
    {
        // Ensure all cameras are inactive except the first one
        foreach (Camera camera in cameras)
        {
            camera.gameObject.SetActive(false);
        }
        if (cameras.Count > 0)
        {
            cameras[currentCameraIndex].gameObject.SetActive(true);  // Activate the first camera
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) // When 'C' is pressed, switch to the next camera
        {
            SwitchCamera();
        }
    }

    void SwitchCamera()
    {
        // Deactivate the current camera
        cameras[currentCameraIndex].gameObject.SetActive(false);

        // Move to the next camera, looping back to the first one when we reach the end
        currentCameraIndex = (currentCameraIndex + 1) % cameras.Count;

        // Activate the next camera
        cameras[currentCameraIndex].gameObject.SetActive(true);
    }
}
