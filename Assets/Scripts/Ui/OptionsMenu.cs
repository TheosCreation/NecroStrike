using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private Button backButton;

    [Header("Master Volume Slider Option")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeText;
    [SerializeField] private Button masterVolumeResetButton;

    [Header("SFX Volume Slider Option")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeText;
    [SerializeField] private Button sfxVolumeResetButton;

    [Header("Music Volume Slider Option")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeText;
    [SerializeField] private Button musicVolumeResetButton;

    [Header("Sensitivity Slider Option")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityText;
    [SerializeField] private Button sensitivityResetButton;

    [Header("Fov Option")]
    [SerializeField] private Slider fovSlider;
    [SerializeField] private TMP_Text fovText;
    [SerializeField] private Button fovResetButton;

    [Header("Look Smoothing Option")]
    [SerializeField] private Toggle lookSmoothingToggle;
    [SerializeField] private Button lookSmoothingResetButton;

    [Header("Tilt Option")]
    [SerializeField] private Toggle tiltToggle;
    [SerializeField] private Button tiltResetButton;

    [Header("Fullscreen Option")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button fullscreenButton;

    [Header("vSync Option")]
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Button vSyncButton;

    [Header("Screen Shake Option")]
    [SerializeField] private Slider screenShakeSlider;
    [SerializeField] private TMP_Text screenShakeText;
    [SerializeField] private Button screenShakeResetButton;

    [Header("Graphics Quality Option")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Button qualityResetButton;

    [Header("Resolution Option")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Button resolutionResetButton;

    private OptionsBase[] options;

    private void Awake()
    {
        options = new OptionsBase[]
        {
            CreateSliderOption(OptionsManager.Instance.options.masterVolume, masterVolumeSlider, masterVolumeText, masterVolumeResetButton, OptionsManager.Instance.UpdateVolume, true),
            CreateSliderOption(OptionsManager.Instance.options.sfxVolume, sfxVolumeSlider, sfxVolumeText, sfxVolumeResetButton, OptionsManager.Instance.UpdateVolume, true),
            CreateSliderOption(OptionsManager.Instance.options.musicVolume, musicVolumeSlider, musicVolumeText, musicVolumeResetButton, OptionsManager.Instance.UpdateVolume, true),
            CreateSliderOption(OptionsManager.Instance.options.sensitivity, sensitivitySlider, sensitivityText, sensitivityResetButton, OptionsManager.Instance.UpdateSensitivity, true),
            CreateSliderOption(OptionsManager.Instance.options.screenShake, screenShakeSlider, screenShakeText, screenShakeResetButton, OptionsManager.Instance.UpdateScreenShake, true),
            CreateSliderOption(OptionsManager.Instance.options.fov, fovSlider, fovText, fovResetButton, OptionsManager.Instance.UpdateFov),

            CreateToggleOption(OptionsManager.Instance.options.lookSmoothing, lookSmoothingToggle, lookSmoothingResetButton, OptionsManager.Instance.UpdateLookSmoothing),
            CreateToggleOption(OptionsManager.Instance.options.tilt, tiltToggle, tiltResetButton, OptionsManager.Instance.UpdateTilt),
            CreateToggleOption(OptionsManager.Instance.options.fullscreen, fullscreenToggle, fullscreenButton, OptionsManager.Instance.UpdateFullscreen),
            CreateToggleOption(OptionsManager.Instance.options.vSync, vSyncToggle, vSyncButton, OptionsManager.Instance.UpdateVSync),

            CreateDropdownOption(OptionsManager.Instance.options.graphicsQuality, qualityDropdown, qualityResetButton, OptionsManager.Instance.UpdateGraphicsQuality),
            CreateDropdownOption(OptionsManager.Instance.options.resolution, resolutionDropdown, resolutionResetButton, OptionsManager.Instance.UpdateScreenResolution)
        };
    }

    private OptionsSlider CreateSliderOption(FloatSetting setting, Slider slider, TMP_Text text, Button resetButton, System.Action<FloatSetting> updateAction, bool isPercentage = false)
    {
        return new OptionsSlider(slider, text, resetButton, value => updateAction(setting), setting, isPercentage);
    }

    private OptionsToggle CreateToggleOption(BoolSetting setting, Toggle toggle, Button resetButton, System.Action<BoolSetting> updateAction)
    {
        return new OptionsToggle(toggle, resetButton, value => updateAction(setting), setting);
    }

    private OptionsDropdown CreateDropdownOption(IntSetting setting, TMP_Dropdown dropdown, Button resetButton, System.Action<IntSetting> updateAction)
    {
        return new OptionsDropdown(dropdown, resetButton, value => updateAction(setting), setting);
    }

    private void OnEnable()
    {
        foreach (var option in options)
        {
            option.Initialize();
        }

        backButton.onClick.AddListener(OpenPreviousPage);
    }

    public void UpdateOptions()
    {
        foreach (var option in options)
        {
            option.Update();
        }
    }

    private void OnDisable()
    {
        foreach (var option in options)
        {
            option.CleanUp();
        }

        backButton.onClick.RemoveListener(OpenPreviousPage);
    }

    private void OpenPreviousPage()
    {
        transform.parent.GetComponent<UiMenuPage>().Back();
    }
}