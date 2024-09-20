using UnityEngine;

public class UiCrosshair : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    private Weapon currentWeapon;

    [SerializeField] private RectTransform mainRectTransform; // Reference to the crosshair RectTransform
    [SerializeField] private float crosshairLocalScale = 1f;  // Base scale for the crosshair
    private Vector2 originalSize = Vector2.zero;

    private void Start()
    {
        // Optionally, find the RectTransform if not assigned
        if (mainRectTransform == null)
        {
            mainRectTransform = GetComponent<RectTransform>();
        }

        originalSize = mainRectTransform.sizeDelta;
    }

    private void Update()
    {
        if (controller == null || controller.weaponHolder == null) return;

        currentWeapon = controller.weaponHolder.currentWeapon;
        if (currentWeapon == null)
        {
            // Reset to the original size if no weapon is equipped
            mainRectTransform.sizeDelta = originalSize;
            return;
        }

        // Assuming currentSpread is a float in Weapon class
        UpdateCrosshairUI(currentWeapon.currentSpread);
    }

    private void UpdateCrosshairUI(float spread)
    {
        if (mainRectTransform == null) return;

        // Adjust crosshair size based on weapon spread
        float adjustedSpread = spread * crosshairLocalScale;

        // Only change sizeDelta to resize without affecting children
        mainRectTransform.sizeDelta = originalSize + new Vector2(adjustedSpread, adjustedSpread);
    }
}