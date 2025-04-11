using UnityEngine;
using System.Collections;

public class Cloud : MonoBehaviour
{
    public Material cloudMaterial;
    public float movementSpeed;

    private CloudPathState currentState;
    private Vector3 initialPosition;
    private Vector3 randomDirection;
    private bool useSineWave;

    private float sineWaveFrequency;
    private float sineWaveAmplitude;
    private float spinSpeed;

    private bool hasEnteredFrustum = false;
    private Renderer rend;

    private int timer = 1000;
    private float updateVisibility = 0.00001f;

    private float fadeInDuration = 1.5f;
    private float visibilityFadeOutDelay = -1f;

    private enum CloudPathState
    {
        Rising,
        PathMotion,
        Exiting
    }

    void Start()
    {
        rend = GetComponent<Renderer>();
        StartCoroutine(CheckFrustumLoop());
    }

    public void Initialize(Vector3 spawnPos, float scale)
    {
        transform.position = spawnPos;
        initialPosition = spawnPos;

        cloudMaterial = CloudMaterials.GetRandomMaterial();
        rend = GetComponent<Renderer>();
        if (cloudMaterial != null && rend != null)
        {
            rend.material = cloudMaterial;
            rend.material.SetFloat("_BoundingBoxSize", scale);
            rend.material.SetFloat("_CloudVisibility", 0f); // start invisible
        }

        Vector3 center = new Vector3(0f, 490f, 0f);
        float dist = Vector3.Distance(spawnPos, center);
        float movementFactor = 0.001f;

        movementSpeed = 0.5f + (dist * movementFactor);
        sineWaveFrequency = 1.0f + dist * movementFactor * 10;
        sineWaveAmplitude = 0.1f + dist * movementFactor * 5;
        spinSpeed = 20f;

        currentState = CloudPathState.Rising;

        Vector2 randXZ = Random.insideUnitCircle.normalized;
        randomDirection = new Vector3(randXZ.x, 1f, randXZ.y);
        useSineWave = true;
    }

    public void UpdateCloud()
    {
        if (!hasEnteredFrustum && IsInAnyFrustum())
        {
            Debug.Log("In frustum");
            hasEnteredFrustum = true;
            visibilityFadeOutDelay = Random.Range(10f, 20f); // start countdown
        }

        // Handle fade in
        if (hasEnteredFrustum)
        {
            float current = cloudMaterial.GetFloat("_CloudVisibility");
            Debug.Log($"Current: {current}");
            float next = Mathf.MoveTowards(current, 1f, Time.deltaTime / fadeInDuration);
            cloudMaterial.SetFloat("_CloudVisibility", next);

            // visibilityFadeOutDelay -= Time.deltaTime;
            // Debug.Log($"visibilityFadeOutDelay: {visibilityFadeOutDelay}");
            // if (visibilityFadeOutDelay <= 0f)
            // {
            //     StartCoroutine(FadeVisibility(1f, 0f, 2f));
            //     visibilityFadeOutDelay = float.PositiveInfinity; // avoid restarting
            // }
        }

        // Move logic
        switch (currentState)
        {
            case CloudPathState.Rising: DoRising(); break;
            case CloudPathState.PathMotion: if (useSineWave) DoSineWave(); else DoSpin(); break;
            case CloudPathState.Exiting: DoExiting(); break;
        }
    }

    private void DoRising()
    {
        transform.position += randomDirection.normalized * movementSpeed * Time.deltaTime;

        if (transform.position.y >= 400f)
        {
            currentState = CloudPathState.PathMotion;
        }
    }

    private void DoSineWave()
    {
        transform.position += Vector3.up * movementSpeed * Time.deltaTime;

        Vector3 pos = transform.position;
        pos.y += Mathf.Sin(Time.time * sineWaveFrequency) * sineWaveAmplitude * Time.deltaTime;
        transform.position = pos;
    }

    private void DoSpin()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
        transform.position += Vector3.up * movementSpeed * Time.deltaTime;
    }

    private void DoExiting()
    {
        transform.position += Vector3.up * (movementSpeed * 2f) * Time.deltaTime;
    }

    public void BeginExit()
    {
        currentState = CloudPathState.Exiting;
    }

    public virtual void Describe()
    {
        Debug.Log($"Cloud: Speed={movementSpeed}, Path={currentState}, usingSine={useSineWave}");
    }

    private IEnumerator CheckFrustumLoop()
    {
        while (!hasEnteredFrustum)
        {
            if (IsInAnyFrustum())
            {
                Debug.Log("Entered Frustum");
                hasEnteredFrustum = true;
                Debug.Log("Setting visibility to 1.0f");
                Renderer rend = GetComponent<Renderer>();
                cloudMaterial = rend.material; // clones the material instance for this renderer
                cloudMaterial.SetFloat("_CloudVisibility", 1.0f);
                // cloudMaterial.SetFloat("_CloudVisibility", 1.0f);
                // StartCoroutine(FadeVisibility(0f, 1f, 1.5f)); // fade in
                float delay = Random.Range(10f, 20f);
                // StartCoroutine(SelfDestructAfter(delay));
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator SelfDestructAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(FadeVisibility(1f, 0f, 2f)); // fade out
    }

    private IEnumerator FadeVisibility(float start, float end, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float v = Mathf.Lerp(start, end, t / duration);
            if (cloudMaterial != null)
                cloudMaterial.SetFloat("_CloudVisibility", v);
            yield return null;
        }
        if (cloudMaterial != null)
            cloudMaterial.SetFloat("_CloudVisibility", end);
    }

    private bool IsInAnyFrustum()
    {
        if (rend == null) return false;
        Camera[] allCams = GameObject.FindObjectsOfType<Camera>();
        foreach (Camera cam in allCams)
        {
            if (cam.name.StartsWith("Cam_Wall"))
            {
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
                if (GeometryUtility.TestPlanesAABB(planes, rend.bounds))
                    return true;
            }
        }
        return false;
    }
}
