using UnityEngine;

public class SunMovement : MonoBehaviour
{
    public float dayDuration = 300f; // 5 minutes for a full day
    public AnimationCurve sunPathCurve;

    private float elapsedTime = 0f;

    void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime > dayDuration)
        {
            elapsedTime = 0f;
        }

        float t = elapsedTime / dayDuration;
        float curvedT = sunPathCurve.Evaluate(t);
        float angle = Mathf.Lerp(-2f, 182f, curvedT);
        transform.rotation = Quaternion.Euler(angle, 0f, 0f);
    }
}

