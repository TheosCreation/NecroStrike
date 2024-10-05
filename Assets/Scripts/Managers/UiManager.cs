using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [SerializeField] private PlayerHud playerHud;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject weaponAttachmentMenuUi;
    [SerializeField] private WeaponAttachmentMenu weaponAttachmentMenu;

    private UiMenuPage currentPage;

    public UiCrosshair crosshair;

    [SerializeField] private FlashImage hitMarker;
    [SerializeField] private FlashImage hurtScreen;
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text ammoReserveText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text roundCounter;
    [SerializeField] private Image playerWeakOverlay;

    //public Image image;
    //public UiBar bar;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        OpenPlayerHud(); 
        HideInteractionPrompt();
    }

    public void OpenPauseMenu()
    {
        currentPage = pauseMenu;
        pauseMenu.gameObject.SetActive(true);
        playerHud.gameObject.SetActive(false);
        weaponAttachmentMenuUi.gameObject.SetActive(false);
        weaponAttachmentMenu.gameObject.SetActive(false);
    }

    public void OpenPlayerHud()
    {
        currentPage = playerHud;

        playerHud.gameObject.SetActive(true);
        deathScreen.SetActive(false);
        pauseMenu.gameObject.SetActive(false);
    }

    public void OpenDeathScreen()
    {
        deathScreen.SetActive(true);
        playerHud.gameObject.SetActive(false);
    }
    public void OpenWeaponAttachmentMenu()
    {
        currentPage = weaponAttachmentMenu;
        weaponAttachmentMenuUi.gameObject.SetActive(true);
        weaponAttachmentMenu.gameObject.SetActive(true);
        pauseMenu.gameObject.SetActive(false);
        playerHud.gameObject.SetActive(false);
    }

    public void UpdateAmmoText(int ammo)
    {
        ammoText.text = ammo.ToString();
    }
    
    public void UpdateAmmoReserveText(int ammo)
    {
        ammoReserveText.text = ammo.ToString();
    }

    public void UpdateSpeedText(float speed)
    {
        speedText.text = speed.ToString("F2");
    }
    
    public void UpdateRoundCount(int count)
    {
        roundCounter.text = count.ToString();
    }

    public void FlashHitMarker()
    {
        hitMarker.Play();
    }

    public void FlashHurtScreen()
    {
        hurtScreen.Play();
    }

    public void SetCrosshair(bool active)
    {
        crosshair.gameObject.SetActive(active);
    }

    public void SetPlayerWeak(bool weak)
    {
        playerWeakOverlay.gameObject.SetActive(weak);
    }

    public void ShowInteractionPrompt(string text)
    {
        interactionText.text = text;
    }   
    
    public void HideInteractionPrompt()
    {
        interactionText.text = "";
    }

    public void Back()
    {
        currentPage.Back();
    }

}