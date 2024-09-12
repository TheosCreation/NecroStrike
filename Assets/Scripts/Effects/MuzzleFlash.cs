using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.5f; // Lifetime of the muzzle flash
    Vector3 initialScale = new Vector3(0.5f, 0.5f, 0.5f);
    Vector3 targetScale = new Vector3(1f, 1f, 1f);
    float timer = 0f;

    void Start()
    {
        // Set the initial scale
        transform.localScale = initialScale;

        // Destroy the muzzle flash after its lifetime
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifeTime);

        // Smoothly interpolate the scale from initial to target
        transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
    }
}