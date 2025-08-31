using UnityEngine;
using System.Collections;

public class Casing : MonoBehaviour
{
    [Header("Force X")]
    [Tooltip("Minimum force on X axis")]
    public float minimumXForce = 25f;
    [Tooltip("Maximum force on X axis")]
    public float maximumXForce = 40f;
    [Header("Force Y")]
    [Tooltip("Minimum force on Y axis")]
    public float minimumYForce = 10f;
    [Tooltip("Maximum force on Y axis")]
    public float maximumYForce = 20f;
    [Header("Force Z")]
    [Tooltip("Minimum force on Z axis")]
    public float minimumZForce = -12f;
    [Tooltip("Maximum force on Z axis")]
    public float maximumZForce = 12f;
    [Header("Rotation Force")]
    [Tooltip("Minimum initial rotation value")]
    public float minimumRotation = -360f;
    [Tooltip("Maximum initial rotation value")]
    public float maximumRotation = 360f;
    [Header("Despawn Time")]
    [Tooltip("How long after spawning that the casing is destroyed")]
    public float despawnTime = 10f;  // Increased the despawn time for realism.

    [Header("Audio")]
    public AudioClip[] casingSounds;
    public AudioSource audioSource;

    [Header("Spin Settings")]
    [Tooltip("How fast the casing spins over time")]
    public float spinSpeed = 2500.0f;

    private bool hasHitGround = false;  // Flag to check if casing has hit the ground.
    [HideInInspector] public Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

    }

    public void Activate()
    {
        // Random rotation of the casing
        rb.AddRelativeTorque(
            Random.Range(minimumRotation, maximumRotation), // X Axis
            Random.Range(minimumRotation, maximumRotation), // Y Axis
            Random.Range(minimumRotation, maximumRotation)  // Z Axis
            * Time.deltaTime);

        // Random direction the casing will be ejected in
        rb.AddRelativeForce(
            Random.Range(minimumXForce, maximumXForce),  // X Axis
            Random.Range(minimumYForce, maximumYForce),  // Y Axis
            Random.Range(minimumZForce, maximumZForce)); // Z Axis
    }

    private void Start()
    {
        // Destroy casings after the despawn time
        Destroy(gameObject, despawnTime);

        // Set random initial rotation
        transform.rotation = Random.rotation;
    }

    private void FixedUpdate()
    {
        // Spin the casing until it hits the ground
        if (!hasHitGround)
        {
            transform.Rotate(Vector3.right, spinSpeed * Time.deltaTime);
            transform.Rotate(Vector3.down, spinSpeed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasHitGround && collision.gameObject.layer == 0)
        {
            hasHitGround = true;  // Mark that the casing has hit the ground.

            // Play the collision sound
            PlaySound();

            // Optionally, you can disable the spin after the first hit.
            spinSpeed = 0f;  // Stop spinning the casing once it hits the ground.
        }
    }

    private void PlaySound()
    {
        if (casingSounds.Length > 0)
        {
            // Get a random casing sound from the array
            audioSource.clip = casingSounds[Random.Range(0, casingSounds.Length)];

            // Play immediately when the casing hits the ground
            audioSource.Play();
        }
    }
}