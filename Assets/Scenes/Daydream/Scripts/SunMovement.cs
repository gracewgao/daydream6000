// using UnityEngine;

// public class SunMovement : MonoBehaviour
// {
//     public float dayDuration = 300f; // 5 minutes
//     private float elapsedTime = 0f;

//     void Update()
//     {
//         elapsedTime += Time.deltaTime;
//         if (elapsedTime > dayDuration)
//         {
//             elapsedTime = 0f; // reset cycle
//         }

//         float angle = (elapsedTime / dayDuration) * 180;
//         transform.rotation = Quaternion.Euler(angle, 0f, 0f);
//     }
// }

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
            elapsedTime = 0f; // reset the cycle
        }

        float t = elapsedTime / dayDuration;
        float easedT = Mathf.SmoothStep(0f, 1f, t);
        float angle = Mathf.Lerp(-10f, 190f, easedT);

        transform.rotation = Quaternion.Euler(angle, 0f, 0f);
    }
}

