using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class OptionsManager : MonoBehaviour
{
    public Options options;
    [SerializeField] private AudioMixer Mixer;
    [HideInInspector] public PlayerController player;
    public static OptionsManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
     
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
         
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.Log("No Player in Scene");
        }

        ApplyAllSettings();
    }

    public void ApplyAllSettings()
    {
        UpdateVolume(options.masterVolume);
        UpdateVolume(options.sfxVolume);
        UpdateVolume(options.musicVolume);
        UpdateSensitivity(options.sensitivity);
        UpdateLookSmoothing(options.lookSmoothing);
        UpdateFov(options.fov);
        UpdateTilt(options.tilt);
        UpdateFullscreen(options.fullscreen);
        UpdateVSync(options.vSync);
        UpdateScreenShake(options.screenShake);
        UpdateGraphicsQuality(options.graphicsQuality);
        UpdateScreenResolution(options.resolution);
    }

    public void UpdateVolume(FloatSetting VolumeSetting)
    {
        // Get the volume percentage from PlayerPrefs
        float volumePercentage = PlayerPrefs.GetFloat(VolumeSetting.name, VolumeSetting.defaultValue);

        // Normalize the volume
        float normalizedVolume = volumePercentage / 100f;

        // Calculate the mixer volume using logarithmic scaling
        float mixerVolume = Mathf.Log10(normalizedVolume <= 0 ? 0.0001f : normalizedVolume) * 20;

        // Set the volume on the audio mixer
        Mixer.SetFloat(VolumeSetting.name, mixerVolume);
    }

    public void UpdateSensitivity(FloatSetting sensitivitySetting)
    {
        if (player == null) return;
        player.playerLook.lookSensitivity = PlayerPrefs.GetFloat(sensitivitySetting.name, sensitivitySetting.defaultValue) / 200;
    }

    public void UpdateLookSmoothing(BoolSetting lookSmoothingSetting)
    {
        if (InputManager.Instance == null) return;

        int defaultValue = lookSmoothingSetting.defaultValue ? 1 : 0;
        int lookSmoothingStatus = PlayerPrefs.GetInt(lookSmoothingSetting.name, defaultValue);
        InputManager.Instance.mouseSmoothingStatus = lookSmoothingStatus != 0;
    }

    public void UpdateFov(FloatSetting fovSetting)
    {
        if (player == null) return;

        float fov = PlayerPrefs.GetFloat(fovSetting.name, fovSetting.defaultValue);
        player.playerLook.fov = fov;
        player.playerLook.ResetZoomLevel();
    }

    public void UpdateTilt(BoolSetting tiltSetting)
    {
        if (player == null) return;

        int defaultValue = tiltSetting.defaultValue ? 1 : 0;
        int tiltStatus = PlayerPrefs.GetInt(tiltSetting.name, defaultValue);
        player.playerLook.tiltStatus = tiltStatus;
    }
    public void UpdateFullscreen(BoolSetting fullscreenSetting)
    {
        int defaultValue = fullscreenSetting.defaultValue ? 1 : 0;
        Screen.fullScreen = PlayerPrefs.GetInt(fullscreenSetting.name, defaultValue) == 1;
    }
    
    public void UpdateVSync(BoolSetting vSyncSetting)
    {
        int defaultValue = vSyncSetting.defaultValue ? 1 : 0;
        QualitySettings.vSyncCount = PlayerPrefs.GetInt(vSyncSetting.name, defaultValue);
    }
    
    public void UpdateScreenShake(FloatSetting screenShakeSetting)
    {
        if (player == null) return;

        float screenShakePercentage = PlayerPrefs.GetFloat(screenShakeSetting.name, screenShakeSetting.defaultValue);
        float normalizedScreenShake = screenShakePercentage / 100f;
        player.playerLook.shakeAmount = normalizedScreenShake;
    }

    public void UpdateGraphicsQuality(IntSetting graphicsQualitySetting)
    {
        // Retrieve the quality level index from PlayerPrefs or use the default value
        int qualityIndex = PlayerPrefs.GetInt(graphicsQualitySetting.name, graphicsQualitySetting.defaultValue);

        // Validate the quality index to ensure it is within valid range
        int maxQualityLevel = QualitySettings.names.Length - 1;
        if (qualityIndex >= 0 && qualityIndex <= maxQualityLevel)
        {
            QualitySettings.SetQualityLevel(qualityIndex, true);
        }
        else
        {
            Debug.LogWarning("Invalid graphics quality index: " + qualityIndex);
        }
    }

    public void UpdateScreenResolution(IntSetting resolutionSetting)
    {
        int resolutionIndex = PlayerPrefs.GetInt(resolutionSetting.name, resolutionSetting.defaultValue);

        Resolution[] availableResolutions = {
            new Resolution { width = 1280, height = 720 },
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 2560, height = 1440 }
        };

        if (resolutionIndex >= 0 && resolutionIndex < availableResolutions.Length)
        {
            Resolution selectedResolution = availableResolutions[resolutionIndex];
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
        }
        else
        {
            Debug.LogWarning("Invalid resolution index: " + resolutionIndex);
        }
    }
}
