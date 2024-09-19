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

    [Header("Weapon Strafe")]
    [SerializeField] private float strafeAmount = 1f;

    private float bobTimer = 0f;
    private Vector3 bobbingOffset;
    private Vector3 strafeOffset;


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
        CalculateStrafe();
        // Calculate the combined final position and rotation including bobbing, strafe, and sway
        Vector3 finalPosition = initialPosition + bobbingOffset + strafeOffset - swayPositionOffset;
        Quaternion finalRotation = initialRotation * swayRotationOffset;

        // Apply the sway, strafe, and bobbing effects to the local position and rotation
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition, Time.deltaTime * swaySmoothness);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, finalRotation, Time.deltaTime * swaySmoothness);

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

        float bobOffsetY = 0;

        // Check if the player is moving and grounded
        Vector3 localVelocity = player.playerMovement.localVelocity;
        if (Mathf.Abs(localVelocity.magnitude) > 0.1f && player.playerMovement.movementController.isGrounded)
        {
            float bobbingReduction = player.weaponHolder.currentWeapon.isAiming ? player.weaponHolder.currentWeapon.motionReduction * 0.4f : 1.0f;

            // Increase the bob timer based on the bobbing speed
            bobTimer += Time.deltaTime * bobbingSpeed * player.playerMovement.movementMultiplier;

            // Calculate the bobbing offset for forward/backward
            bobOffsetY = Mathf.Sin(bobTimer) * bobbingAmount * 0.1f * bobbingReduction;         // Forward/backward bobbing
        }

        // Apply the final bobbing offset
        bobbingOffset = new Vector3(0, bobOffsetY, 0);
    }
    private void CalculateStrafe()
    {
        if (player.weaponHolder.currentWeapon == null) return;

        float strafeOffsetX = 0;

        // Check if the player is moving and grounded
        float inputX = InputManager.Instance.MovementVector.x;
        if (Mathf.Abs(inputX) > 0.1f && player.playerMovement.movementController.isGrounded)
        {
            float strafeReduction = player.weaponHolder.currentWeapon.isAiming ? player.weaponHolder.currentWeapon.motionReduction * 0.4f : 1.0f;

            // Apply static offset based on left/right movement
            float strafeDirection = Mathf.Sign(inputX); // +1 for right, -1 for left
            strafeOffsetX = strafeDirection * strafeAmount * strafeReduction; // Static amount offset
        }

        // Apply the final strafe offset
        strafeOffset = new Vector3(strafeOffsetX, 0, 0);
    }
}