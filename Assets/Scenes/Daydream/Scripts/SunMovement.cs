using UnityEngine;

public class SunMovement : MonoBehaviour
{
    public float dayDuration = 300f; // 5 minutes
    private float elapsedTime = 0f;

    void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime > dayDuration)
        {
            elapsedTime = 0f; // reset cycle
        }

        float rotationX = Mathf.Lerp(0f, 180f, elapsedTime / dayDuration);
        transform.rotation = Quaternion.Euler(rotationX, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
    }
}
