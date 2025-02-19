using UnityEngine;

public class CardSpin : MonoBehaviour
{
    public float baseRotationSpeed = 30f; // Adjust rotation speed
    private float rotationSpeed;
    private Vector3 rotationAxis;
    private float randomXTilt; // Small random X-axis tilt

    void Start()
    {
        // ✅ Randomize speed slightly so they don’t spin identically
        rotationSpeed = baseRotationSpeed + Random.Range(-5f, 5f);

        // ✅ Main spin on Y-axis, but with a small tilt on X
        rotationAxis = new Vector3(Random.Range(-1f, 1f), 1f, 0f).normalized;

        // ✅ Apply a small random tilt to the X rotation on start
        randomXTilt = Random.Range(-12f, 12f); // Slight tilt range
        transform.Rotate(randomXTilt, 0, 0);
    }

    void Update()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.World);
    }
}
