using UnityEngine;

public class TemporaryVisualEffect : MonoBehaviour
{
    private float lifetime = 15f;
    private float scaleDuration = 3f;
    private float timer = 0f;
    private Vector3 targetScale;
    private Vector3 initialScale;

    void Start()
    {
        targetScale = transform.localScale;
        initialScale = Vector3.zero;
        transform.localScale = initialScale;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // rotation
        float swayAmount = Mathf.Sin(Time.time * 0.5f) * 5f;
        transform.Rotate(swayAmount * Time.deltaTime, swayAmount * Time.deltaTime, swayAmount * Time.deltaTime);

        // scale up
        if (timer <= scaleDuration)
        {
            float t = timer / scaleDuration;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
        }
        // stay full scale
        else if (timer <= lifetime - scaleDuration)
        {
            transform.localScale = targetScale;
        }
        // scale down
        else
        {
            float t = (timer - (lifetime - scaleDuration)) / scaleDuration;
            transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
        }

        // destructor
        if (timer >= lifetime)
        {
            // remove CloudLightTracker when mesh lifetime over
            var tracker = gameObject.GetComponent<CloudLightTracker>();
            if (tracker != null) Destroy(tracker);

            Destroy(gameObject);
        }
    }
}
