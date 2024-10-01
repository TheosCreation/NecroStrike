using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponAnim : MonoBehaviour
{
    private PlayerController player;
    [Header("Weapon Sway")]
    [SerializeField] private float positionalSway = 1f;
    [SerializeField] private float rotationalSway = 1f;
    [SerializeField] private float swaySmoothness = 1f;

    [Header("Walking Bobbing")]
    [SerializeField] private float bobbingFrequency = 1.5f;
    [SerializeField] private float verticalBobbingAmplitude = 0.05f;
    [SerializeField] private float horizontalBobbingAmplitude = 0.05f;
    [SerializeField] private float bobbingHorizontalSmoothing = 5f;

    private Vector3 initialPosition = Vector3.zero;
    private Quaternion initialRotation = Quaternion.identity;
    private Vector3 swayPositionOffset = Vector3.zero;
    private Quaternion swayRotationOffset = Quaternion.identity;
    private float bobbingTimer = 0f; 
    private float targetBobbingDirection = 1f;
    private float currentBobbingDirection = 1f;


    private void Awake()
    {
        player = transform.root.GetComponent<PlayerController>();
    }

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    private void Update()
    {
        CalculateSway();
        Vector3 bobbingOffset = CalculateBobbing();

        // Calculate the combined final position and rotation including bobbing, strafe, and sway
        Vector3 finalPosition = initialPosition - swayPositionOffset + bobbingOffset;
        Quaternion finalRotation = initialRotation * swayRotationOffset;

        // Apply the sway, strafe, and bobbing effects to the local position and rotation
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition, Time.deltaTime * swaySmoothness);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, finalRotation, Time.deltaTime * swaySmoothness);
    }

    private void CalculateSway()
    {
        if (player.weaponHolder.currentWeapon == null) return;

        float swayReduction = player.weaponHolder.currentWeapon.isAiming ? player.weaponHolder.currentWeapon.motionReduction : 1.0f;

        float mouseX = InputManager.Instance.currentMouseDelta.x * 0.1f;
        float mouseY = InputManager.Instance.currentMouseDelta.y * 0.1f;

        swayPositionOffset = new Vector3(mouseX, mouseY, 0) * positionalSway * swayReduction;
        swayRotationOffset = Quaternion.Euler(new Vector3(-mouseY, mouseX, 0) * rotationalSway * swayReduction);
    }

    private Vector3 CalculateBobbing()
    {
        if (player.weaponHolder.currentWeapon == null) return Vector3.zero;

        float bobbingReduction = player.weaponHolder.currentWeapon.isAiming ? player.weaponHolder.currentWeapon.motionReduction : 1.0f;

        Vector3 movementVector = InputManager.Instance.MovementVector;

        if (movementVector.magnitude > 0.1f) // Check if the player is moving
        {
            // Set target bobbing direction based on initial x input
            if (movementVector.x < 0)
            {
                targetBobbingDirection = -1f; // Moving left
            }
            else if (movementVector.x > 0)
            {
                targetBobbingDirection = 1f; // Moving right
            }

            // Smoothly interpolate towards the target direction
            currentBobbingDirection = Mathf.Lerp(currentBobbingDirection, targetBobbingDirection, Time.deltaTime * bobbingHorizontalSmoothing);

            // Increment bobbing timer
            bobbingTimer += Time.deltaTime * bobbingFrequency;

            // Calculate vertical bobbing (up/down)
            float verticalOffset = -Mathf.Abs(verticalBobbingAmplitude * Mathf.Sin(bobbingTimer)) * 0.001f;

            // Calculate horizontal bobbing (left/right) with lerped direction
            float horizontalOffset = 0.001f * horizontalBobbingAmplitude * Mathf.Cos(bobbingTimer) * currentBobbingDirection;

            // Apply bobbing reduction for aiming and return the final bobbing vector
            return new Vector3(horizontalOffset * bobbingReduction, verticalOffset * bobbingReduction, 0);
        }
        else
        {
            // Reset the timer and bobbing direction when not moving
            bobbingTimer = 0f;
            return Vector3.zero;
        }
    }
}
