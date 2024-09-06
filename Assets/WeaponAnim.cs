using UnityEngine;

public class WeaponAnim : MonoBehaviour
{
    private PlayerController player;
    [Header("Weapon Sway")]
    [SerializeField] private float positionalSway = 1f;
    [SerializeField] private float rotationalSway = 1f;
    [SerializeField] private float swaySmoothness = 1f;

    private Vector3 initialPosition = Vector3.zero;
    private Quaternion initialRotation = Quaternion.identity;

    [Header("Weapon Bobbing")]
    [SerializeField] private float bobbingSpeed = 1f;
    [SerializeField] private float bobbingAmount = 1f;

    private float bobTimer = 0f;

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
        ApplySway();
        ApplyBobbing();
    }

    private void ApplySway()
    {
        if (player.weaponHolder.currentWeapon == null) return;

        // Determine if the player is aiming and set a sway reduction multiplier
        float swayReduction = player.weaponHolder.currentWeapon.isAiming ? player.weaponHolder.currentWeapon.motionReduction : 1.0f;

        // Scale down the input values for more subtle sway effects
        float mouseX = InputManager.Instance.currentMouseDelta.x * 0.1f;
        float mouseY = InputManager.Instance.currentMouseDelta.y * 0.1f;

        // Apply the sway reduction multiplier
        Vector3 positionOffset = new Vector3(mouseX, mouseY, 0) * positionalSway * swayReduction;
        Quaternion rotationOffset = Quaternion.Euler(new Vector3(-mouseY, mouseX, 0) * rotationalSway * swayReduction);

        transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition - positionOffset, Time.deltaTime * swaySmoothness);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, initialRotation * rotationOffset, Time.deltaTime * swaySmoothness);
    }

    private void ApplyBobbing()
    {
        if (player.weaponHolder.currentWeapon == null) return;

        float moveSpeed = Mathf.Abs(player.playerMovement.horizontalMovementSpeed.magnitude);
        float bobOffset = 0;

        if(moveSpeed > 0.1f && player.playerMovement.movementController.isGrounded && !player.weaponHolder.currentWeapon.isAiming)
        {
            bobTimer += Time.deltaTime * bobbingSpeed;
            bobOffset = Mathf.Sin(bobTimer) * bobbingAmount * 0.1f;
        }
        else
        {
            bobTimer = 0;
            bobOffset = Mathf.Lerp(bobTimer, 0, Time.deltaTime * swaySmoothness);
        }

        transform.localPosition += new Vector3(0, bobOffset, 0);
    }

}