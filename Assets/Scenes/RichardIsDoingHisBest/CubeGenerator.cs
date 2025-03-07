using UnityEngine;

public class CubeGenerator : MonoBehaviour
{
    public float roomLength = 8.84f;  // 29 feet
    public float roomWidth = 8.53f;   // 28 feet
    public float roomHeight = 3.06f;  // Corrected height

    public bool renderRoom = false;

    public Material Mat1;
    public Material Mat2;
    public Material Mat3;
    public Material Mat4;

    void Start()
    {
        // Activate all available displays (not needed for room rendering but kept for future projection setup)
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }

        // Create walls and floor (without cameras)
        CreateWall(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), new Vector3(0, roomHeight, 0), displayPort: 4, "Floor");  
        // CreateWall(new Vector3(0, roomHeight, 0), Quaternion.Euler(180, 0, 0), "Ceiling"); 
        CreateWall(new Vector3(0, roomHeight / 2, roomWidth / 2), Quaternion.Euler(-90, 0, 0), new Vector3(0, roomHeight / 2, 0), displayPort: 0, "Front");  
        CreateWall(new Vector3(0, roomHeight / 2, -roomWidth / 2), Quaternion.Euler(90, 0, 0), new Vector3(0, roomHeight / 2, 0), displayPort: 1, "Back"); 
        CreateWall(new Vector3(roomLength / 2, roomHeight / 2, 0), Quaternion.Euler(-90, 90, 0), new Vector3(0, roomHeight / 2, 0), displayPort: 2, "Right"); 
        CreateWall(new Vector3(-roomLength / 2, roomHeight / 2, 0), Quaternion.Euler(-90, -90, 0), new Vector3(0, roomHeight / 2, 0), displayPort: 3, "Left"); 
    }

    void CreateWall(Vector3 position, Quaternion rotation, Vector3 camPosition, int displayPort, string label, Rect? customViewPort = null)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Plane);
        wall.transform.position = position;
        wall.transform.rotation = rotation;
        wall.name = label;  // Naming for debugging

        // Scale based on wall type
        if (label == "Front") {
            wall.transform.localScale = new Vector3(roomLength / 10, 1, roomHeight / 10);
            wall.GetComponent<Renderer>().material = Mat1;
        } else if (label == "Back") {
            wall.transform.localScale = new Vector3(roomLength / 10, 1, roomHeight / 10);
            wall.GetComponent<Renderer>().material = Mat3;
        } else if (label == "Right") {
            wall.transform.localScale = new Vector3(roomWidth / 10, 1, roomHeight / 10);
            wall.GetComponent<Renderer>().material = Mat2;
        } else if (label == "Left") {
            wall.transform.localScale = new Vector3(roomWidth / 10, 1, roomHeight / 10);
            wall.GetComponent<Renderer>().material = Mat4;
        } else { // Floor & Ceiling
            wall.transform.localScale = new Vector3(roomLength / 10, 1, roomWidth / 10);
        }

        if (label == "Floor") {
            int resolutionWidth = 3690;
            int resolutionHeight = 2970;
            float aspectRatio = (float)resolutionWidth / resolutionHeight;

            // Calculate horizontal FOV based on room width and camera distance
            float horizontalFOV = 2 * Mathf.Atan((roomWidth / 2) / roomHeight) * Mathf.Rad2Deg;

            // Adjust vertical FOV based on the aspect ratio
            float verticalFOV = 2 * Mathf.Atan(Mathf.Tan(horizontalFOV * Mathf.Deg2Rad / 2) / aspectRatio) * Mathf.Rad2Deg;

            // Create and configure camera
            GameObject camObj = new GameObject("Camera_" + label);
            Camera cam = camObj.AddComponent<Camera>();
            camObj.transform.position = camPosition;
            camObj.transform.LookAt(wall.transform.position);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.targetDisplay = displayPort;
            cam.fieldOfView = verticalFOV;
            if (customViewPort != null)
            {
                cam.rect = customViewPort.Value;
            }
        } else if (label == "Front" || label == "Back") {
            int resolutionWidth = 2970;
            int resolutionHeight = 1080;
            float aspectRatio = (float)resolutionWidth / resolutionHeight;

            // Calculate horizontal FOV based on room width and camera distance
            float horizontalFOV = 2 * Mathf.Atan((roomWidth / 2) / roomHeight) * Mathf.Rad2Deg;

            // Adjust vertical FOV based on the aspect ratio
            float verticalFOV = 2 * Mathf.Atan(Mathf.Tan(horizontalFOV * Mathf.Deg2Rad / 2) / aspectRatio) * Mathf.Rad2Deg;

            // Create and configure camera
            GameObject camObj = new GameObject("Camera_" + label);
            Camera cam = camObj.AddComponent<Camera>();
            camObj.transform.position = camPosition;
            camObj.transform.LookAt(wall.transform.position);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.targetDisplay = displayPort;
            cam.fieldOfView = verticalFOV;
            if (customViewPort != null)
            {
                cam.rect = customViewPort.Value;
            }
        } else {
            int resolutionWidth = 3126;
            int resolutionHeight = 1080;
            float aspectRatio = (float)resolutionWidth / resolutionHeight;

            // Calculate horizontal FOV based on room width and camera distance
            float horizontalFOV = 2 * Mathf.Atan((roomWidth / 2) / roomHeight) * Mathf.Rad2Deg;

            // Adjust vertical FOV based on the aspect ratio
            float verticalFOV = 2 * Mathf.Atan(Mathf.Tan(horizontalFOV * Mathf.Deg2Rad / 2) / aspectRatio) * Mathf.Rad2Deg;

            // Create and configure camera
            GameObject camObj = new GameObject("Camera_" + label);
            Camera cam = camObj.AddComponent<Camera>();
            camObj.transform.position = camPosition;
            camObj.transform.LookAt(wall.transform.position);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.targetDisplay = displayPort;
            cam.fieldOfView = verticalFOV;
            if (customViewPort != null)
            {
                cam.rect = customViewPort.Value;
            }
        }
    }
}
