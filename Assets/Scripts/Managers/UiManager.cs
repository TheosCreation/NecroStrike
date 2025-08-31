using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [Header("References Setup")]
    [SerializeField] private PlayerHud playerHud;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject weaponAttachmentMenuUi;
    [SerializeField] private WeaponAttachmentMenu weaponAttachmentMenu;

    private UiMenuPage currentPage;

    public UiCrosshair crosshair;

    //[SerializeField] private FlashImage hitMarker;
    [SerializeField] private FlashImage hurtScreen;
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text ammoReserveText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text roundCounter;
    [SerializeField] private Image playerWeakOverlay;

    [Header("Prefabs Setup")]
    [SerializeField] private FloatingText floatingTextPrefab;

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
        //speedText.text = speed.ToString("F2");
    }
    
    public void UpdateRoundCount(int count)
    {
        roundCounter.text = count.ToString();
    }

    public void UpdatePoints(int _points, int _pointChange)
    {
        totalScoreText.text = _points.ToString();
        FloatingText floatingText = Instantiate(floatingTextPrefab, totalScoreText.transform);

        float RandomX = Random.Range(-40f, 40f);
        float RandomY = Random.Range(-20f, 20f);
        floatingText.transform.localPosition = new Vector3(RandomX, 50f + RandomY, 0.0f);

        string text = "";
        Color color = Color.white;
        if (_pointChange > 0)
        {
            text = "+" + _pointChange.ToString();
            color = Color.green;
        }
        else
        {
            text = _pointChange.ToString();
            color = Color.red;
        }
        floatingText.Init(text, color);
    }

    public void FlashHitMarker()
    {
        //hitMarker.Play();
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