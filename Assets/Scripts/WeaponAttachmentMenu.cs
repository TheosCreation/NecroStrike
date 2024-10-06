using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class WeaponAttachmentMenu : UiMenuPage
{
    [SerializeField] private PlayerController player; //current player so we can get the weapon in the weapon holder
    [SerializeField] private Button backButton;
    [SerializeField] private Transform weaponAttachmentPoint;
    [SerializeField] float rotateSpeed = 0.1f;
    [SerializeField] float rotationXMin = -10f;
    [SerializeField] float rotationXMax = 10f;
    [SerializeField] private Camera m_Camera;
    [SerializeField] float fovMin = 20f;
    [SerializeField] float fovChange = 1f;
    [SerializeField] float fovMax = 30f;
    public bool isRotating = false;
    [SerializeField] private Transform attachButtonsTransform;
    [SerializeField] private InventoryButton inventoryButtonPrefab;
    private List<InventoryButton> buttons = new List<InventoryButton>();

    private Weapon currentlyDisplayedWeapon;

    private void Awake()
    {
        InputManager.Instance.playerInput.Ui.Click.started += _ctx => StartRotating();
        InputManager.Instance.playerInput.Ui.Click.canceled += _ctx => EndRotating();
        InputManager.Instance.playerInput.Ui.Zoom.performed += _ctx => Zoom(_ctx.ReadValue<Vector2>());
    }

    private bool hasInitialized = false;

    private void Start()
    {
        hasInitialized = true;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        backButton.onClick.AddListener(Back);

        if (!hasInitialized) return;

        if (player.playerLook.playerCamera != null)
        {
            player.playerLook.playerCamera.enabled = false;
        }
        player.weaponHolder.canAttach = false;

        currentlyDisplayedWeapon = player.weaponHolder.currentWeapon;

        foreach (Weapon weapon in player.weaponHolder.weapons)
        {
            weapon.SetAnimationActive(false);

            weapon.gameObject.SetActive(true);// set the weapon active for the preview
            // Generate preview texture for the weapon
            Texture2D previewTexture = AssetPreview.GetAssetPreview(weapon.gameObject);

            // Check if the preview texture is generated
            if (previewTexture != null)
            {
                // Ensure that texture has alpha support and enable transparency
                previewTexture.alphaIsTransparency = true;

                // Create a new button for this weapon (assuming InventoryButton is a class with a Button reference)
                InventoryButton newButton = Instantiate(inventoryButtonPrefab, attachButtonsTransform);
                buttons.Add(newButton);
                newButton.SetText(weapon.name);

                // Set the button's image using the generated preview texture with transparency
                newButton.button.image.sprite = Sprite.Create(previewTexture,
                    new Rect(0, 0, previewTexture.width, previewTexture.height),
                    new Vector2(0.5f, 0.5f));

                newButton.button.onClick.AddListener(() => EnableWeaponObject(weapon));
            }

            // Attach the weapon to the correct attachment point in the scene
            weapon.transform.parent = weaponAttachmentPoint;
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            // Disable the weapon (or any component of the weapon if needed)
            weapon.gameObject.SetActive(false);
            weapon.enabled = false;
        }

        player.weaponHolder.SelectCurrentWeapon();
    }
    private void StartRotating()
    {
        isRotating = true;
    }

    private void Update()
    {
        if(isRotating)
        {
            RotateWeapon();
        }
    }

    private void EnableWeaponObject(Weapon _weapon)
    {
        foreach (Weapon weapon in player.weaponHolder.weapons)
        {
            weapon.gameObject.SetActive(false);
        }
        currentlyDisplayedWeapon = _weapon;
        _weapon.gameObject.SetActive(true);
    }

    private void RotateWeapon()
    {
        Vector2 currentMouseDelta = InputManager.Instance.currentMouseDelta;
        weaponAttachmentPoint.localEulerAngles += new Vector3(-currentMouseDelta.y, -currentMouseDelta.x, 0f) * rotateSpeed;

        float localEulerAnglesX = weaponAttachmentPoint.localEulerAngles.x;
        if (localEulerAnglesX > 180f)
        {
            localEulerAnglesX -= 360f;
        }
        float rotationX = Mathf.Clamp(localEulerAnglesX, rotationXMin, rotationXMax);

        weaponAttachmentPoint.localEulerAngles = new Vector3(rotationX, weaponAttachmentPoint.localEulerAngles.y, weaponAttachmentPoint.localEulerAngles.z);
    }

    private void EndRotating()
    {
        isRotating = false;
    }

    private void Zoom(Vector2 _direction)
    {
        if (_direction.y > 0)
        {
            m_Camera.fieldOfView -= fovChange;
        }
        else if (_direction.y < 0)
        {
            m_Camera.fieldOfView += fovChange;
        }
        m_Camera.fieldOfView = Mathf.Clamp(m_Camera.fieldOfView, fovMin, fovMax);
    }

    public override void Back()
    {
        UiManager.Instance.OpenPauseMenu();
    }

    private void OnDisable()
    {
        backButton.onClick.RemoveListener(Back);

        if (!hasInitialized) return;
        
        EndRotating();
        if (player.playerLook.playerCamera != null)
        {
            player.playerLook.playerCamera.enabled = true;
        }
        player.weaponHolder.canAttach = true;

        foreach (Weapon weapon in player.weaponHolder.weapons)
        {
            weapon.transform.parent = player.weaponHolder.idlePos;
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity; 
            weapon.enabled = true;
            weapon.SetAnimationActive(true);
        }
        player.weaponHolder.SelectCurrentWeapon();

        CleanUpButtons();
    }
    private void CleanUpButtons()
    {
        if (buttons != null && buttons.Count > 0)
        {
            foreach (InventoryButton inventoryButton in buttons)
            {
                inventoryButton.button.onClick.RemoveAllListeners();
                if(inventoryButton.button != null)
                {
                    Destroy(inventoryButton.gameObject);
                }
            }

            // Clear the list after destroying
            buttons.Clear();
        }
    }
    public override void OpenMainPage()
    {
    }
}