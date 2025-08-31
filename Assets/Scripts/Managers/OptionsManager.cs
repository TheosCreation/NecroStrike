using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.DebugUI;

public enum OptionType
{
    PlayerSensitivity,
    MouseSmoothing,
    Fullscreen,
    FieldOfView,
    vSync,
    MusicVolume,
    SfxVolume,
    MasterVolume,
    ScreenShake,
    CameraTilt,
    InvertX,
    InvertY,
    GraphicsQuality,
    Resolution,
    AutoRespawn,
}

public class OptionsManager : SingletonPersistent<OptionsManager>
{
    public static Dictionary<OptionType, object> defaultOptions = new Dictionary<OptionType, object>
    {
        { OptionType.PlayerSensitivity, 30f },
        { OptionType.MouseSmoothing, 0f },
        { OptionType.Fullscreen, true },
        { OptionType.FieldOfView, 85f },
        { OptionType.vSync, true },
        { OptionType.MusicVolume, 1f },
        { OptionType.SfxVolume, 1f },
        { OptionType.MasterVolume, 1f },
        { OptionType.ScreenShake, 1f },
        { OptionType.CameraTilt, true },
        { OptionType.InvertX, false },
        { OptionType.InvertY, false },
        { OptionType.GraphicsQuality, 2 },
        { OptionType.Resolution, 0 },
        { OptionType.AutoRespawn, false }
    };

    private static readonly Resolution[] allResoultions = new[]
    {
        new Resolution { width = 640, height = 480 },
        new Resolution { width = 720, height = 480 },
        new Resolution { width = 720, height = 576 },
        new Resolution { width = 800, height = 600 },
        new Resolution { width = 1024, height = 768 },
        new Resolution { width = 1176, height = 664 },
        new Resolution { width = 1280, height = 768 },
        new Resolution { width = 1280, height = 800 },
        new Resolution { width = 1280, height = 1024 },
        new Resolution { width = 1360, height = 768 },
        new Resolution { width = 1366, height = 768 },
        new Resolution { width = 1440, height = 900 },
        new Resolution { width = 1440, height = 1080 },
        new Resolution { width = 1600, height = 900 },
        new Resolution { width = 1600, height = 1024 },
        new Resolution { width = 1600, height = 1200 },
        new Resolution { width = 1680, height = 1050 },
        new Resolution { width = 1920, height = 1080 },
        new Resolution { width = 1920, height = 1200 },
        new Resolution { width = 1920, height = 1440 },
        new Resolution { width = 2560, height = 1080 },
        new Resolution { width = 2560, height = 1440 },
        new Resolution { width = 3440, height = 1440 },
        new Resolution { width = 3840, height = 2160 },
        new Resolution { width = 5120, height = 2160 },
        new Resolution { width = 7680, height = 4320 }
    };

    public static Resolution[] supportedResolutions = new[]
    {
        new Resolution { width = 640, height = 480 }
    };


    public bool GetOptionsDropownList(string tag, out string[] options)
    {
        if (tag == "Resolution")
        {
            options = supportedResolutions.Select(r => $"{r.width}x{r.height}").ToArray();
            return true;
        }
        else if (tag == "Quality")
        {
            options = QualitySettings.names;
            return true;
        }
        options = null;
        return false;
    }

    public Task Init()
    {
        List<Resolution> supported = new List<Resolution>();
        Resolution[] systemSupported = Screen.resolutions;

        foreach (var preset in allResoultions)
        {
            foreach (var system in systemSupported)
            {
                if (preset.width == system.width && preset.height == system.height)
                {
                    //Debug.Log(preset);
                    supported.Add(preset);
                    break;
                }
            }
        }

        supportedResolutions = supported.ToArray();

        //Retrieve resoultion size and set the default resolution to the closest to max supported by monitor
        Resolution current = Screen.currentResolution;
        int closestIndex = 0;
        int smallestDifference = int.MaxValue;

        for (int i = 0; i < supportedResolutions.Length; i++)
        {
            Resolution candidate = supportedResolutions[i];
            int diff = Mathf.Abs(current.width - candidate.width) + Mathf.Abs(current.height - candidate.height);

            if (diff < smallestDifference)
            {
                smallestDifference = diff;
                closestIndex = i;
            }
        }

        defaultOptions[OptionType.Resolution] = closestIndex;

        UpdateScreenResolution();

        return Task.CompletedTask;
    }

    private void Start()
    {
        Screen.fullScreen = PrefsManager.Instance.GetBool(OptionType.Fullscreen);
        QualitySettings.vSyncCount = (PrefsManager.Instance.GetBool(OptionType.vSync) ? 1 : 0);

        QualitySettings.SetQualityLevel(PrefsManager.Instance.GetInt(OptionType.GraphicsQuality));

        UpdateScreenResolution();
    }

    private void UpdateScreenResolution()
    {
        int resolutionIndex = PrefsManager.Instance.GetInt(OptionType.Resolution);
        if (resolutionIndex < 0 || resolutionIndex >= supportedResolutions.Length)
        {
            resolutionIndex = 0;
        }

        Resolution selected = supportedResolutions[resolutionIndex];
        Screen.SetResolution(selected.width, selected.height, Screen.fullScreenMode);
    }

    public void UpdateOption<T>(OptionType type, T value)
    {
        // Update the preferences based on type.
        if (value is int intValue)
        {
            PrefsManager.Instance.SetInt(type, intValue);
        }
        else if (value is float floatValue)
        {
            PrefsManager.Instance.SetFloat(type, floatValue);
        }
        else if (value is bool boolValue)
        {
            PrefsManager.Instance.SetBool(type, boolValue);
        }
        else if (value is string stringValue)
        {
            PrefsManager.Instance.SetString(type, stringValue);
        }
        else
        {
            Debug.LogError($"Unsupported type {typeof(T)} for option update.");
            return; // Exit early to prevent further execution.
        }

        UpdateOptionInternal(type, value);
    }


    //get rid of this lol
    private void UpdateOptionInternal<T>(OptionType type, T value)
    {

        // Update audio mixer volume if applicable
        switch (type)
        {
            case OptionType.MouseSmoothing:
                // Update mouse smoothing settings.
                if (InputManager.Instance && value is float mouseSmooth)
                {
                    Debug.Log("Updated MouseSmoothing");
                    InputManager.Instance.mouseSmoothTime = mouseSmooth;
                }
                break;
            case OptionType.Fullscreen:
                // Update fullscreen settings (e.g., toggle full screen mode).
                if (value is bool fullScreen)
                {
                    Debug.Log("Updated Fullscreen");
                    Screen.fullScreen = fullScreen;
                }
                break;
            case OptionType.vSync:
                // Update vertical sync settings.
                Debug.Log("Updated vSync");
                if (value is bool vSync)
                {
                    QualitySettings.vSyncCount = (vSync ? 1 : 0);
                }
                break;
            case OptionType.MusicVolume:
                if (value is float musicVolume)
                {
                    MusicManager.Instance.SetVolume("MusicVolume", musicVolume);
                }
                else
                {
                    Debug.LogError("MusicVolume requires a float value.");
                }
                break;
            case OptionType.SfxVolume:
                if (value is float sfxVolume)
                {
                    MusicManager.Instance.SetVolume("SFXVolume", sfxVolume);
                }
                else
                {
                    Debug.LogError("SfxVolume requires a float value.");
                }
                break;
            case OptionType.MasterVolume:
                if (value is float masterVolume)
                {
                    MusicManager.Instance.SetVolume("MasterVolume", masterVolume);
                }
                else
                {
                    Debug.LogError("MasterVolume requires a float value.");
                }
                break;
            case OptionType.GraphicsQuality:
                // Update graphics quality settings.
                if (value is int qualityLevel)
                {
                    Debug.Log("Updated GraphicsQuality");
                    QualitySettings.SetQualityLevel(qualityLevel);
                }
                break;
            case OptionType.Resolution:
                // Update screen resolution settings.
                Debug.Log("Updated Resolution");
                if (value is int resolution)
                {
                    Resolution selected = supportedResolutions[resolution];
                    Screen.SetResolution(selected.width, selected.height, Screen.fullScreenMode);
                }
                break;
            default:
                //Debug.LogError("Unhandled option type: " + type);
                break;
        }
    }

    public T ResetOption<T>(OptionType type)
    {
        if (defaultOptions.ContainsKey(type))
        {
            object value = defaultOptions[type];
            if (value is T typedValue)
            {
                // Update the preferences manager with the default value using the proper type-specific method.
                if (typedValue is int intValue)
                {
                    PrefsManager.Instance.SetInt(type, intValue);
                }
                else if (typedValue is float floatValue)
                {
                    PrefsManager.Instance.SetFloat(type, floatValue);
                }
                else if (typedValue is bool boolValue)
                {
                    PrefsManager.Instance.SetBool(type, boolValue);
                }
                else if (typedValue is string stringValue)
                {
                    PrefsManager.Instance.SetString(type, stringValue);
                }
                else
                {
                    Debug.LogError($"Unsupported type for resetting default value: {typeof(T)}");
                }


                UpdateOptionInternal(type, typedValue);

                return typedValue;
            }
            else
            {
                Debug.LogError($"Default value for {type} is not of type {typeof(T)}. Actual type: {value.GetType()}.");
            }
        }
        else
        {
            Debug.LogError("No default value set for type: " + type);
        }
        return default;
    }


    public void ResetAllOptions()
    {
        foreach (OptionType type in defaultOptions.Keys)
        {
            object defaultValue = defaultOptions[type];
            if (defaultValue is int)
            {
                ResetOption<int>(type);
            }
            else if (defaultValue is float)
            {
                ResetOption<float>(type);
            }
            else if (defaultValue is bool)
            {
                ResetOption<bool>(type);
            }
            else if (defaultValue is string)
            {
                ResetOption<string>(type);
            }
            else
            {
                Debug.LogError($"Unsupported default value type for {type}: {defaultValue.GetType()}");
            }
        }
        Debug.Log("All options have been reset.");
    }
}
