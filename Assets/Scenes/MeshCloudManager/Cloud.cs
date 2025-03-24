using UnityEngine;

public class Cloud : MonoBehaviour
{
    // references
    public Material cloudMaterial;

    // movement settings
    public Vector3 movementDirection;
    public float movementSpeed;

    // called by CloudSystem (or itself) after instantiation
    public void Initialize()
    {
        // randomly pick one of the available materials
        cloudMaterial = CloudMaterials.GetRandomMaterial();
        if (cloudMaterial != null)
        {
            // Apply the material to the renderer on this GameObject (if it has one)
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = cloudMaterial;
            }
        }

        // generate random movement direction and speed
        movementDirection = Random.insideUnitSphere; // or maybe 2D XY-plane only, etc.
        movementSpeed = Random.Range(0.05f, 0.15f);

        // If you need to do anything else at creation time, do it here
    }

    // Movement handling is done by the CloudSystem, so we just have an optional Describe:
    public virtual void Describe()
    {
        Debug.Log($"Cloud: Speed={movementSpeed}, Direction={movementDirection}");
    }
}

