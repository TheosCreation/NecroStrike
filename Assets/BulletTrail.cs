using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    public float lifeTime = 2.0f; // Total lifetime of the bullet trail
    public float speed = 1.0f; // Speed of the bullet trail in units per second
    private Vector3 startPosition;
    private Vector3 hitPosition;
    private float distance;

    [SerializeField] private Impact impactPrefab;

    public void Init(Vector3 _hitpoint, Vector3 _hitNormal)
    {
        hitPosition = _hitpoint;
        startPosition = transform.position; // Store the initial position of the bullet
        distance = Vector3.Distance(startPosition, hitPosition); // Calculate the total distance to travel

        Vector3 offset = _hitNormal * 0.01f;
        Instantiate(impactPrefab, hitPosition + offset, Quaternion.LookRotation(_hitNormal));

        Destroy(gameObject, lifeTime); // Destroy the bullet trail after its lifetime expires
    }

    private void Update()
    {
        // Calculate the direction from the start position to the hit position
        Vector3 direction = (hitPosition - startPosition).normalized;

        // Move the bullet trail at a constant speed
        transform.position += direction * speed * Time.deltaTime;

        // Check if the bullet trail has reached or passed the hit position
        if (Vector3.Distance(startPosition, transform.position) >= distance)
        {
            transform.position = hitPosition; // Snap to the hit position
            Destroy(gameObject); // Destroy the bullet trail once it reaches the destination
        }
    }
}