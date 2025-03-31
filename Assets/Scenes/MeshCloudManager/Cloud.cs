using UnityEngine;

public class Cloud : MonoBehaviour
{
    public Material cloudMaterial;
    public float movementSpeed;

    private CloudPathState currentState;

    private Vector3 initialPosition;
    private Vector3 randomDirection; // For Rising
    private bool useSineWave;

    // Sine wave parameters
    private float sineWaveFrequency;
    private float sineWaveAmplitude;

    // Rotation speed if spinning
    private float spinSpeed;

    private enum CloudPathState
    {
        Rising,
        PathMotion,
        Exiting
    }

    /// <summary>
    /// Called by CloudSystem after instantiation.
    /// Now takes 'spawnPos' as argument.
    /// </summary>
    public void Initialize(Vector3 spawnPos)
    {
        // position & record
        transform.position = spawnPos;
        initialPosition = spawnPos;

        // apply a material
        cloudMaterial = CloudMaterials.GetRandomMaterial();
        if (cloudMaterial != null)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.material = cloudMaterial;
        }

        // derive movement parameters from the position
        // e.g., distance from the center point.
        Vector3 center = new Vector3(0f, 410f, 0f); // center point
        float dist = Vector3.Distance(spawnPos, center);

        // example formula: movementSpeed is 0.05f + some fraction of 'dist'
        float movementFactor = 0.001f;
        movementSpeed = 0.5f + (dist * movementFactor);  // tweak the movementFactor (0.001f) as you wish

        // sine wave frequency/amplitude also scale with dist
        sineWaveFrequency = 1.0f + dist * movementFactor * 10;   // e.g. bigger distance => bigger frequency
        sineWaveAmplitude = 0.1f + dist * movementFactor * 5;

        // spin speed scaled by distance
        spinSpeed = 20f; // or random within a range plus dist

        Debug.Log($"SpawnPos={spawnPos}, dist={dist}, movementSpeed={movementSpeed}, spin={spinSpeed}");

        // all clouds start in the "rising" state
        currentState = CloudPathState.Rising;

        // create a random “rising angle” with an upward Y
        Vector2 randXZ = Random.insideUnitCircle.normalized;
        randomDirection = new Vector3(randXZ.x, 1f, randXZ.y);

        // randomly decide if we do sine wave or spin
        useSineWave = (Random.value > 0.5f);
    }

    /// <summary>
    /// Custom per-cloud update logic (called by CloudSystem).
    /// </summary>
    public void UpdateCloud()
    {
        Debug.Log($"current position: {transform.position}");
        Debug.Log($"current state: {currentState}");
        switch (currentState)
        {
            case CloudPathState.Rising:
                DoRising();
                break;

            case CloudPathState.PathMotion:
                if (useSineWave) DoSineWave();
                else DoSpin();
                break;

            case CloudPathState.Exiting:
                DoExiting();
                break;
        }
    }

    private void DoRising()
    {
        // Debug.Log($"Reached DoRising with increase: {randomDirection.normalized * movementSpeed * Time.deltaTime}");
        // Move upward from y=445 to y=455 at the random direction
        transform.position += randomDirection.normalized * movementSpeed * Time.deltaTime * 10; // TODO: remove 10
        
        // Once we exceed y=455, switch to the path motion
        if (transform.position.y >= 400f)
        {
            // Debug.Log("Switching path motion");
            // Vector3 pos = transform.position;
            // pos.y = 400f;
            // transform.position = pos;

            currentState = CloudPathState.PathMotion;
        }
    }

    private void DoSineWave()
    {
        // Debug.Log("Reached DoSineWave");

        // move upward slowly
        transform.position += Vector3.up * movementSpeed * Time.deltaTime;

        // add a sine offset for a particular coordinate
        Vector3 pos = transform.position;
        pos.y += Mathf.Sin(Time.time * sineWaveFrequency) * sineWaveAmplitude * Time.deltaTime;
        transform.position = pos;
    }

    private void DoSpin()
    {
        // spin around local y axis while also moving upward
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
        transform.position += Vector3.up * movementSpeed * Time.deltaTime;
    }

    private void DoExiting()
    {
        // for now, let them drift “off screen,” just move them up
        transform.position += Vector3.up * (movementSpeed * 2f) * Time.deltaTime;  
    }

    /// <summary>
    /// Called by CloudSystem when it wants to transition this cloud out
    /// and eventually remove it.
    /// </summary>
    public void BeginExit()
    {
        currentState = CloudPathState.Exiting;
    }

    /// Debug / optional
    public virtual void Describe()
    {
        Debug.Log($"Cloud: Speed={movementSpeed}, Path={currentState}, usingSine={useSineWave}");
    }
}
