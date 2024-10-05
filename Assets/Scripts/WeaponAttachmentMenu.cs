using UnityEngine;
using UnityEngine.UI;

public class WeaponAttachmentMenu : UiMenuPage
{
    [SerializeField] private PlayerController player; //current player so we can get the weapon in the weapon holder
    [SerializeField] private Button backButton;
    [SerializeField] private Transform weaponAttachmentPoint;
    [SerializeField] float rotateSpeed = 0.1f;
    [SerializeField] float rotationYMin = -10f;
    [SerializeField] float rotationYMax = 10f;
    public bool isRotating = false;

    private Weapon currentlyDisplayedWeapon;

    private void Awake()
    {
        InputManager.Instance.playerInput.Ui.Click.started += _ctx => StartRotating();
        InputManager.Instance.playerInput.Ui.Click.canceled += _ctx => EndRotating();
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

        player.playerLook.playerCamera.enabled = false;
        player.weaponHolder.canAttach = false;

        currentlyDisplayedWeapon = player.weaponHolder.currentWeapon;

        foreach (Weapon weapon in player.weaponHolder.weapons)
        {
            weapon.transform.parent = weaponAttachmentPoint;
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.enabled = false;
        }
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
    private void RotateWeapon()
    {
        Vector2 currentMouseDelta = InputManager.Instance.currentMouseDelta;
        float rotationY = Mathf.Clamp(currentMouseDelta.y, rotationYMin, rotationYMax);
        weaponAttachmentPoint.localEulerAngles += new Vector3(-rotationY, -currentMouseDelta.x, 0f) * rotateSpeed;
    }

    private void EndRotating()
    {
        isRotating = false;
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
        player.playerLook.playerCamera.enabled = true;
        player.weaponHolder.canAttach = true;

        foreach (Weapon weapon in player.weaponHolder.weapons)
        {
            weapon.transform.parent = player.weaponHolder.idlePos;
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity; 
            weapon.enabled = true;
        }
    }

    public override void OpenMainPage()
    {
    }
}