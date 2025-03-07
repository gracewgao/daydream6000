using UnityEngine;
using System.Collections.Generic;

public class EyepoolCubeGenerator : MonoBehaviour
{
    public float roomLength = 8.84f;  // 29 feet
    public float roomWidth = 8.53f;   // 28 feet
    public float roomHeight = 3.06f;  // Corrected height
    private Dictionary<int, Dictionary<string, List<Camera>>> camerasByDisplay = new Dictionary<int, Dictionary<string, List<Camera>>>();
    private bool defaultValues = false;

    public Material Mat1;
    public Material Mat2;
    public Material Mat3;
    public Material Mat4;
    public Material FloorMat;

    void Start()
    {
        // Activate all available displays
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }

        // Create walls and floor (without cameras)
        CreateWall(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), new Vector3(0, roomHeight, 0), displayPort: 2, "Floor");
        CreateWall(new Vector3(0, roomHeight / 2, roomWidth / 2), Quaternion.Euler(-90, 0, 0), new Vector3(0, roomHeight, 0), displayPort: 0, "Front");
        CreateWall(new Vector3(0, roomHeight / 2, -roomWidth / 2), Quaternion.Euler(90, 0, 0), new Vector3(0, roomHeight, 0), displayPort: 0, "Back");
        CreateWall(new Vector3(roomLength / 2, roomHeight / 2, 0), Quaternion.Euler(-90, 90, 0), new Vector3(0, roomHeight, 0), displayPort: 1, "Right");
        CreateWall(new Vector3(-roomLength / 2, roomHeight / 2, 0), Quaternion.Euler(-90, -90, 0), new Vector3(0, roomHeight, 0), displayPort: 1, "Left");
        SetupCameras();
    }

    void CreateWall(Vector3 position, Quaternion rotation, Vector3 camPosition, int displayPort, string label)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Plane);
        wall.transform.position = position;
        wall.transform.rotation = rotation;
        wall.name = label;  // Naming for debugging

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
        } else { // Floor
            wall.transform.localScale = new Vector3(roomLength / 10, 1, roomWidth / 10);
            wall.GetComponent<Renderer>().material = FloorMat;
        }
    }

    void SetupCameras()
    {
        float floorCameraOffset = roomHeight * 0.1f;
        int displayPort = 2;
        string label = "FloorCamera";
        for (int i = 0; i < 6; i++)
        {
            if (i >= 4)
            {
                displayPort = 3;
            }
            Vector3 position = new Vector3(
                (i % 3 - 1) * roomLength / 3,
                floorCameraOffset,
                ((int)(i / 3) * 2 - 1) * roomWidth / 3);
            Quaternion rotation = Quaternion.Euler(90, 0, 0);

            CreateCamera(position, "_left", rotation, label + (i/2), displayPort);
            if (i >= 4) {
                CreateCamera(position, "_left", rotation, label + (i / 2), displayPort);
            }
        }

        int displayPortFrontBack = 0;
        int displayPortLeftRight = 1;

        // LEFT SIDE VECTORS
        Vector3 rightWallLeftQuadrant = new Vector3(-1.8f, 1.53f, -1.64f);
        Vector3 rightWallRightQuadrant = new Vector3(-1.8f, 1.53f, 1.64f);

        Vector3 leftWallLeftQuadrant = new Vector3(1.8f, 1.53f, 1.64f);
        Vector3 leftWallRightQuadrant = new Vector3(1.8f, 1.53f, -1.64f);

        Vector3 frontWallLeftQuadrant = new Vector3(1.78f, 1.53f, -1.64f);
        Vector3 frontWallRightQuadrant = new Vector3(-1.78f, 1.53f, -1.64f);

        Vector3 backWallLeftQuadrant = new Vector3(-1.78f, 1.53f, 1.64f);
        Vector3 backWallRightQuadrant = new Vector3(1.78f, 1.53f, 1.64f);

        // if (defaultValues)
        // {
        //     rightWallLeftQuadrant = new Vector3(roomLength / 2 + 0.1f, roomHeight * 0.5f, 0);
        //     rightWallRightQuadrant = new Vector3(roomLength / 2 + 0.1f, roomHeight * 0.5f, 0);
            
        //     leftWallLeftQuadrant = new Vector3(-roomLength / 2 - 0.1f, roomHeight * 0.5f, 0);
        //     leftWallRightQuadrant = new Vector3(-roomLength / 2 - 0.1f, roomHeight * 0.5f, 0);

        //     frontWallLeftQuadrant = new Vector3(0, roomHeight * 0.5f, roomWidth / 2 + 0.1f);
        //     frontWallRightQuadrant = new Vector3(0, roomHeight * 0.5f, roomWidth / 2 + 0.1f);

        //     backWallLeftQuadrant = new Vector3(0, roomHeight * 0.5f, -roomWidth / 2 - 0.1f);
        //     backWallRightQuadrant = new Vector3(0, roomHeight * 0.5f, -roomWidth / 2 - 0.1f);
        // }

        // RIGHT SIDE VECTORS
        string left = "_left";
        string right = "_right";
        CreateCamera(frontWallLeftQuadrant, right, Quaternion.Euler(0, 180, 0), "FrontCamera", displayPortFrontBack);
        CreateCamera(backWallLeftQuadrant, right, Quaternion.Euler(0, 0, 0), "BackCamera", displayPortLeftRight);
        CreateCamera(rightWallLeftQuadrant, right, Quaternion.Euler(0, -90, 0), "RightCamera", displayPortFrontBack);
        CreateCamera(leftWallLeftQuadrant, right, Quaternion.Euler(0, 90, 0), "LeftCamera", displayPortLeftRight);
        
        CreateCamera(frontWallRightQuadrant, left, Quaternion.Euler(0, 180, 0), "FrontCamera", displayPortFrontBack);
        CreateCamera(backWallRightQuadrant, left, Quaternion.Euler(0, 0, 0), "BackCamera", displayPortLeftRight);
        CreateCamera(rightWallRightQuadrant, left, Quaternion.Euler(0, -90, 0), "RightCamera", displayPortFrontBack);
        CreateCamera(leftWallRightQuadrant, left, Quaternion.Euler(0, 90, 0), "LeftCamera", displayPortLeftRight);
    }

    void CreateCamera(Vector3 position, string direction, Quaternion rotation, string label, int displayPort)
    {
        GameObject camObj = new GameObject(label + direction);
        Camera cam = camObj.AddComponent<Camera>();
        camObj.transform.position = position;
        // cam.fieldOfView = fov;
        camObj.transform.rotation = rotation;
        cam.targetDisplay = displayPort;
        cam.fieldOfView = CalculateFOVForPosition(position);

        if (!camerasByDisplay.ContainsKey(displayPort))
            camerasByDisplay[displayPort] = new Dictionary<string, List<Camera>>();
        if (!camerasByDisplay[displayPort].ContainsKey(label))
            camerasByDisplay[displayPort][label] = new List<Camera>();

        camerasByDisplay[displayPort][label].Add(cam);
        SetViewport(cam, displayPort, label);
    }

    void SetViewport(Camera cam, int displayPort, string label)
    {
        var camerasInDisplay = camerasByDisplay[displayPort];
        int verticalIndex = 0, horizontalIndex = 0;
        int totalVertical = camerasInDisplay.Count;
        int totalHorizontal = camerasInDisplay[label].Count;

        foreach (var kvp in camerasInDisplay)
        {
            if (kvp.Key == label) break;
            verticalIndex++;
        }
        horizontalIndex = camerasInDisplay[label].IndexOf(cam);

        float width = 0.5f;
        float height = 0.5f;
        float x = horizontalIndex * width;
        float y = 1f - (verticalIndex + 1) * height;

        cam.rect = new Rect(x, y, width, height);
    }

    float CalculateFOVForPosition(Vector3 position)
    {
        return 60f;
    }
}
