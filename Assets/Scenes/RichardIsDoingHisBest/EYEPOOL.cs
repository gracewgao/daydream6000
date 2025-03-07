using UnityEngine;

public class EYEPOOL : MonoBehaviour
{
    public float roomLength = 8.84f;  // 29 feet
    public float roomWidth = 8.53f;   // 28 feet
    public float roomHeight = 3.06f;  // Corrected height

    void Start()
    {
        // Activate all available displays
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }

        // Create walls and floor (without cameras)
        CreateWall(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), Color.cyan, new Vector3(0, roomHeight, 0), displayPort: 2, "Floor");
        CreateWall(new Vector3(0, roomHeight / 2, roomWidth / 2), Quaternion.Euler(-90, 0, 0), Color.red, new Vector3(0, roomHeight, 0), displayPort: 0, "Front");
        CreateWall(new Vector3(0, roomHeight / 2, -roomWidth / 2), Quaternion.Euler(90, 0, 0), Color.green, new Vector3(0, roomHeight, 0), displayPort: 0, "Back");
        CreateWall(new Vector3(roomLength / 2, roomHeight / 2, 0), Quaternion.Euler(-90, 90, 0), Color.blue, new Vector3(0, roomHeight, 0), displayPort: 1, "Right");
        CreateWall(new Vector3(-roomLength / 2, roomHeight / 2, 0), Quaternion.Euler(-90, -90, 0), Color.yellow, new Vector3(0, roomHeight, 0), displayPort: 1, "Left");
        SetupCameras();
    }

    void CreateWall(Vector3 position, Quaternion rotation, Color color, Vector3 camPosition, int displayPort, string label)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Plane);
        wall.transform.position = position;
        wall.transform.rotation = rotation;
        wall.name = label;  // Naming for debugging

        // Scale based on wall type
        if (label == "Front" || label == "Back")
            wall.transform.localScale = new Vector3(roomLength / 10, 1, roomHeight / 10);
        else if (label == "Right" || label == "Left")
            wall.transform.localScale = new Vector3(roomWidth / 10, 1, roomHeight / 10);
        else // Floor & Ceiling
            wall.transform.localScale = new Vector3(roomLength / 10, 1, roomWidth / 10);

        // Assign color
        wall.GetComponent<Renderer>().material.color = color;
    }

    void SetupCameras()
    {
        // Floor Cameras (6 cameras)
        float floorCameraOffset = roomHeight * 0.1f; // Slightly above the floor
        for (int i = 0; i < 6; i++)
        {
            Vector3 position = new Vector3(
                (i % 3 - 1) * roomLength / 3,
                floorCameraOffset,
                ((int)(i / 3) * 2 - 1) * roomWidth / 3);
            Quaternion rotation = Quaternion.Euler(90, 0, 0);  // Pointing upwards
            CreateCamera(position, rotation, "FloorCamera_" + i);
        }

        // Wall Cameras (2 for each wall)
        float wallCameraHeight = roomHeight * 0.5f;
        float wallCameraDepthOffset = 0.1f; // Slightly off the wall
        CreateCamera(new Vector3(0, wallCameraHeight, roomWidth / 2 + wallCameraDepthOffset), Quaternion.Euler(0, 180, 0), "FrontCamera_1");
        CreateCamera(new Vector3(0, wallCameraHeight, -roomWidth / 2 - wallCameraDepthOffset), Quaternion.Euler(0, 0, 0), "BackCamera_1");
        CreateCamera(new Vector3(roomLength / 2 + wallCameraDepthOffset, wallCameraHeight, 0), Quaternion.Euler(0, -90, 0), "RightCamera_1");
        CreateCamera(new Vector3(-roomLength / 2 - wallCameraDepthOffset, wallCameraHeight, 0), Quaternion.Euler(0, 90, 0), "LeftCamera_1");

        // Adjust for second camera on each wall
        CreateCamera(new Vector3(0, wallCameraHeight, roomWidth / 2 + wallCameraDepthOffset), Quaternion.Euler(0, 180, 0), "FrontCamera_2");
        CreateCamera(new Vector3(0, wallCameraHeight, -roomWidth / 2 - wallCameraDepthOffset), Quaternion.Euler(0, 0, 0), "BackCamera_2");
        CreateCamera(new Vector3(roomLength / 2 + wallCameraDepthOffset, wallCameraHeight, 0), Quaternion.Euler(0, -90, 0), "RightCamera_2");
        CreateCamera(new Vector3(-roomLength / 2 - wallCameraDepthOffset, wallCameraHeight, 0), Quaternion.Euler(0, 90, 0), "LeftCamera_2");
    }

    void CreateCamera(Vector3 position, Quaternion rotation, string name)
    {
        GameObject camObj = new GameObject(name);
        Camera cam = camObj.AddComponent<Camera>();
        camObj.transform.position = position;
        camObj.transform.rotation = rotation;
        cam.fieldOfView = CalculateFOVForPosition(position);  // Custom method to set FOV based on position
    }

    float CalculateFOVForPosition(Vector3 position)
    {
        // Placeholder for dynamic FOV calculation based on position
        return 60f;  // Example FOV, should be adjusted based on actual needs
    }
}
