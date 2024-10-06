using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class FloatingText : MonoBehaviour
{
    private TMP_Text text;
    [SerializeField] private float floatSpeed = 20f;
    [SerializeField] private float horizontalSpeedMax = 20f;
    [SerializeField] private float fadeDuration = 1.5f; 
    [SerializeField] private float floatDuration = 0.2f;
    private float fadeStartTime;               // Time when the fade starts
    private bool isFading = false;

    private Timer floatTimer;                  // Timer for the float duration
    private float horizontalSkew;              // Random horizontal skew value

    private void Awake()
    {
        text = GetComponent<TMP_Text>();

        // Random skew for slight left and right movement
        horizontalSkew = Random.Range(-horizontalSpeedMax, horizontalSpeedMax); // Random skew factor
    }

    public void Init(string message, Color _textColor)
    {
        text.text = message;
        text.color = _textColor;

        // Start floating immediately
        floatTimer = gameObject.AddComponent<Timer>();

        // Start the floating timer and set up a callback for when floating ends
        floatTimer.SetTimer(floatDuration, OnFloatComplete);
    }

    private void Update()
    {
        // Apply upward and horizontal movement while not fading
        Vector3 upwardMovement = Vector3.up * floatSpeed * Time.deltaTime;
        Vector3 horizontalMovement = Vector3.right * horizontalSkew * Time.deltaTime;
        transform.Translate(upwardMovement + horizontalMovement);

        // Handle fading effect
        if (isFading)
        {
            float elapsedFadeTime = Time.time - fadeStartTime;
            float fadeRatio = Mathf.Clamp01(elapsedFadeTime / fadeDuration);

            // Update the alpha of the text
            Color currentColor = text.color;
            currentColor.a = Mathf.Lerp(1f, 0f, fadeRatio);
            text.color = currentColor;

            // If fully faded, destroy the game object
            if (currentColor.a <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    // Callback for when the floating phase is complete
    private void OnFloatComplete()
    {
        fadeStartTime = Time.time;
        isFading = true;
    }
}