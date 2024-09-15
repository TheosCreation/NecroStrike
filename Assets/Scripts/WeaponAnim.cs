using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponAnim : MonoBehaviour
{
    private PlayerController player;
    [Header("Weapon Sway")]
    [SerializeField] private float positionalSway = 1f;
    [SerializeField] private float rotationalSway = 1f;
    [SerializeField] private float swaySmoothness = 1f;

    private Vector3 initialPosition = Vector3.zero;
    private Quaternion initialRotation = Quaternion.identity;
    private Vector3 swayPositionOffset = Vector3.zero;
    private Quaternion swayRotationOffset = Quaternion.identity;

    [Header("Weapon Bobbing")]
    [SerializeField] private float bobbingSpeed = 1f;
    [SerializeField] private float bobbingAmount = 1f;

    private float bobTimer = 0f;
    private Vector3 bobbingOffset;


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
        CalculateBobbing();

        // Apply the sway effects to the local position and rotation
        transform.localPosition = Vector3.Lerp(transform.localPosition, (initialPosition + bobbingOffset) - swayPositionOffset, Time.deltaTime * swaySmoothness);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, initialRotation * swayRotationOffset, Time.deltaTime * swaySmoothness);
    }

    private void CalculateSway()
    {
        if (player.weaponHolder.currentWeapon == null) return;

        // Determine if the player is aiming and set a sway reduction multiplier
        float swayReduction = player.weaponHolder.currentWeapon.isAiming ? player.weaponHolder.currentWeapon.motionReduction : 1.0f;

        // Scale down the input values for more subtle sway effects
        float mouseX = InputManager.Instance.currentMouseDelta.x * 0.1f;
        float mouseY = InputManager.Instance.currentMouseDelta.y * 0.1f;

        // Apply the sway reduction multiplier
        swayPositionOffset = new Vector3(mouseX, mouseY, 0) * positionalSway * swayReduction;
        swayRotationOffset = Quaternion.Euler(new Vector3(-mouseY, mouseX, 0) * rotationalSway * swayReduction);
    }

    private void CalculateBobbing()
    {
        if (player.weaponHolder.currentWeapon == null) return;

        // Get the movement vector from the InputManager
        Vector2 movementVector = InputManager.Instance.MovementVector;
        float bobOffsetX = 0;
        float bobOffsetY = 0;

        // Check if the player is moving and grounded
        if (movementVector.magnitude > 0.1f && player.playerMovement.movementController.isGrounded)
        {
            float bobbingReduction = player.weaponHolder.currentWeapon.isAiming ? player.weaponHolder.currentWeapon.motionReduction * 0.4f : 1.0f;

            // Increase the bob timer based on the bobbing speed
            bobTimer += Time.deltaTime * bobbingSpeed * player.playerMovement.movementMultiplier;

            // Calculate the bobbing offset for both X (side-to-side) and Y (up-and-down)
            bobOffsetX = Mathf.Sin(bobTimer * 0.5f) * bobbingAmount * movementVector.x * 0.1f * bobbingReduction; // Side-to-side bobbing
            bobOffsetY = Mathf.Sin(bobTimer) * bobbingAmount * movementVector.y * 0.1f * bobbingReduction; // Up-and-down bobbing
        }

        bobbingOffset = new Vector3(bobOffsetX, bobOffsetY, 0);
    }
}