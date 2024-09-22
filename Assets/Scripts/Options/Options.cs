using UnityEngine;

[System.Serializable]
public struct FloatSetting
{
    public string name;
    public float defaultValue;

    public FloatSetting(string name, float defaultValue)
    {
        this.name = name;
        this.defaultValue = defaultValue;
    }
}

[System.Serializable]
public struct IntSetting
{
    public string name;
    public int defaultValue;

    public IntSetting(string name, int defaultValue)
    {
        this.name = name;
        this.defaultValue = defaultValue;
    }
}

[System.Serializable]
public struct BoolSetting
{
    public string name;
    public bool defaultValue;

    public BoolSetting(string name, bool defaultValue)
    {
        this.name = name;
        this.defaultValue = defaultValue;
    }
}

[CreateAssetMenu(fileName = "NewOptions", menuName = "ScriptableObjects/NewOptions", order = 1)]
public class Options : ScriptableObject
{
    public FloatSetting masterVolume = new FloatSetting("MasterVolume", 100.0f);
    public FloatSetting musicVolume = new FloatSetting("MusicVolume", 100.0f);
    public FloatSetting sfxVolume = new FloatSetting("SFXVolume", 100.0f);
    public FloatSetting sensitivity = new FloatSetting("Sensitivity", 1.0f);
    public BoolSetting lookSmoothing = new BoolSetting("LookSmoothing", false);
    public FloatSetting fov = new FloatSetting("Fov", 105.0f);
    public BoolSetting tilt = new BoolSetting("Tilt", true);
    public BoolSetting vSync = new BoolSetting("VSync", true);
    public BoolSetting fullscreen = new BoolSetting("Fullscreen", true);
    public FloatSetting screenShake = new FloatSetting("ScreenShake", 100.0f);
    public IntSetting graphicsQuality = new IntSetting("GraphicsQuility", 2); //0-Very Low, 1-Low, 2-Medium, 3-High, 4-Very High, 5-Ultra
    public IntSetting resolution = new IntSetting("Resolution", 1); //0-1280x720, 1-1920x1080, 2-2560x1440
}