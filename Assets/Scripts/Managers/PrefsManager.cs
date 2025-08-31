using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using Newtonsoft.Json;

public class PrefsManager : SingletonPersistent<PrefsManager>
{
    private enum PrefsCommitMode
    {
        Immediate = 0,
        OnQuit = 1,
        DirtySlowTick = 2
    }

    private FileStream prefsStream;

    private FileStream localPrefsStream;

    private static PrefsCommitMode CommitMode = ((!Application.isEditor) ? PrefsCommitMode.DirtySlowTick : PrefsCommitMode.Immediate);

    private float timeSinceLastTick;

    private const float SlowTickCommitInterval = 3f;

    private const bool DebugLogging = false;

    private bool isDirty;

    private bool isLocalDirty;

    public static int monthsSinceLastPlayed = 0;

    public static Action<OptionType, object> onPrefChanged;

    public Dictionary<string, object> prefMap;

    public Dictionary<string, object> localPrefMap;

    private readonly Dictionary<string, Func<object, object>> propertyValidators = new Dictionary<string, Func<object, object>>
    {
        {
            "difficulty",
            delegate(object value)
            {
                if (!(value is int num))
                {
                    Debug.LogWarning("Difficulty value is not an int");
                    return 2;
                }
                if (num < 0 || num > 4)
                {
                    Debug.LogWarning("Difficulty validation error");
                    return 4;
                }
                return (object)null;
            }
        }
    };

    private static string prefsNote = "LocalPrefs.json contains preferences local to this machine. It does NOT get backed up in any way.\nPrefs.json contains preferences that are synced across all of your machines via Steam Cloud.\n\nAll prefs files must be valid json.\nIf you edit them, make sure you don't break the format.\n\nIf a pref key is missing from the prefs files, the game will use the default value and save ONLY if overridden.\nAdditionally, you can NOT move things between Prefs.json and LocalPrefs.json, the game will ignore them if misplaced.";

    public static string PrefsPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Preferences");

    public bool HasKey(OptionType option)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (!prefMap.ContainsKey(key))
        {
            return localPrefMap.ContainsKey(key);
        }
        return true;
    }

    public void DeleteKey(OptionType option)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (prefMap.ContainsKey(key))
        {
            prefMap.Remove(key);
            if (CommitMode == PrefsCommitMode.DirtySlowTick)
            {
                isDirty = true;
            }
            if (CommitMode == PrefsCommitMode.Immediate)
            {
                CommitPrefs(local: false);
            }
        }
        if (localPrefMap.ContainsKey(key))
        {
            localPrefMap.Remove(key);
            if (CommitMode == PrefsCommitMode.DirtySlowTick)
            {
                isLocalDirty = true;
            }
            if (CommitMode == PrefsCommitMode.Immediate)
            {
                CommitPrefs(local: true);
            }
        }
    }

    public bool GetBoolLocal(OptionType option, bool fallback = false)
    {
        EnsureInitialized();
        if (localPrefMap.TryGetValue(option.ToString(), out var value) && value is bool)
        {
            return (bool)value;
        }
        if (OptionsManager.defaultOptions.TryGetValue(option, out var value2) && value2 is bool)
        {
            return (bool)value2;
        }
        return fallback;
    }

    public bool GetBool(OptionType option, bool fallback = false)
    {
        EnsureInitialized();
        if (prefMap.TryGetValue(option.ToString(), out var value) && value is bool)
        {
            return (bool)value;
        }
        if (OptionsManager.defaultOptions.ContainsKey(option))
        {
            object obj = OptionsManager.defaultOptions[option];
            if (obj is bool)
            {
                return (bool)obj;
            }
        }
        return fallback;
    }

    public void SetBoolLocal(OptionType option, bool content)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (localPrefMap.ContainsKey(key))
        {
            localPrefMap[key] = content;
        }
        else
        {
            localPrefMap.Add(key, content);
        }
        if (CommitMode == PrefsCommitMode.Immediate)
        {
            CommitPrefs(local: true);
        }
        if (CommitMode == PrefsCommitMode.DirtySlowTick)
        {
            isLocalDirty = true;
        }
        onPrefChanged?.Invoke(option, content);
    }

    public void SetBool(OptionType option, bool content)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (prefMap.ContainsKey(key))
        {
            prefMap[key] = content;
        }
        else
        {
            prefMap.Add(key, content);
        }
        if (CommitMode == PrefsCommitMode.Immediate)
        {
            CommitPrefs(local: false);
        }
        if (CommitMode == PrefsCommitMode.DirtySlowTick)
        {
            isDirty = true;
        }
        onPrefChanged?.Invoke(option, content);
    }

    public int GetIntLocal(OptionType option, int fallback = 0)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (localPrefMap.TryGetValue(key, out var value))
        {
            if (value is int num)
            {
                return (int)EnsureValid(key, num);
            }
            if (value is long num2)
            {
                return (int)EnsureValid(key, (int)num2);
            }
            if (value is float num3)
            {
                return (int)EnsureValid(key, (int)num3);
            }
            if (value is double num4)
            {
                return (int)EnsureValid(key, (int)num4);
            }
        }
        if (OptionsManager.defaultOptions.TryGetValue(option, out var value2) && value2 is int)
        {
            return (int)value2;
        }
        return fallback;
    }
    public int GetInt(string key, int fallback = 0)
    {
        EnsureInitialized();
        if (prefMap.TryGetValue(key, out var value))
        {
            if (value is int num)
            {
                return (int)EnsureValid(key, num);
            }
            if (value is long num2)
            {
                return (int)EnsureValid(key, (int)num2);
            }
            if (value is float num3)
            {
                return (int)EnsureValid(key, (int)num3);
            }
            if (value is double num4)
            {
                return (int)EnsureValid(key, (int)num4);
            }
        }
        return fallback;
    }

    public int GetInt(OptionType option, int fallback = 0)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (prefMap.TryGetValue(key, out var value))
        {
            if (value is int num)
            {
                return (int)EnsureValid(key, num);
            }
            if (value is long num2)
            {
                return (int)EnsureValid(key, (int)num2);
            }
            if (value is float num3)
            {
                return (int)EnsureValid(key, (int)num3);
            }
            if (value is double num4)
            {
                return (int)EnsureValid(key, (int)num4);
            }
        }
        if (OptionsManager.defaultOptions.TryGetValue(option, out var value2) && value2 is int)
        {
            return (int)value2;
        }
        return fallback;
    }

    public void SetIntLocal(OptionType option, int content)
    {
        EnsureInitialized();
        string key = option.ToString();
        content = (int)EnsureValid(key, content);
        if (localPrefMap.ContainsKey(key))
        {
            localPrefMap[key] = content;
        }
        else
        {
            localPrefMap.Add(key, content);
        }
        if (CommitMode == PrefsCommitMode.Immediate)
        {
            CommitPrefs(local: true);
        }
        if (CommitMode == PrefsCommitMode.DirtySlowTick)
        {
            isLocalDirty = true;
        }
        onPrefChanged?.Invoke(option, content);
    }

    public void SetInt(OptionType option, int content)
    {
        EnsureInitialized();
        string key = option.ToString();
        content = (int)EnsureValid(key, content);
        if (prefMap.ContainsKey(key))
        {
            prefMap[key] = content;
        }
        else
        {
            prefMap.Add(key, content);
        }
        if (CommitMode == PrefsCommitMode.Immediate)
        {
            CommitPrefs(local: false);
        }
        if (CommitMode == PrefsCommitMode.DirtySlowTick)
        {
            isDirty = true;
        }
        onPrefChanged?.Invoke(option, content);
    }

    public float GetFloatLocal(OptionType option, float fallback = 0f)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (localPrefMap.ContainsKey(key))
        {
            if (localPrefMap[key] is float num)
            {
                return (float)EnsureValid(key, num);
            }
            if (localPrefMap[key] is int num2)
            {
                return (int)EnsureValid(key, num2);
            }
            if (localPrefMap[key] is long num3)
            {
                return (long)EnsureValid(key, num3);
            }
            if (localPrefMap[key] is double num4)
            {
                return (float)EnsureValid(key, (float)num4);
            }
        }
        if (OptionsManager.defaultOptions.ContainsKey(option))
        {
            object obj = OptionsManager.defaultOptions[option];
            if (obj is float)
            {
                return (float)obj;
            }
        }
        return fallback;
    }

    public float GetFloat(OptionType option, float fallback = 0f)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (prefMap.ContainsKey(key))
        {
            if (prefMap[key] is float num)
            {
                return (float)EnsureValid(key, num);
            }
            if (prefMap[key] is int num2)
            {
                return (int)EnsureValid(key, num2);
            }
            if (prefMap[key] is long num3)
            {
                return (long)EnsureValid(key, num3);
            }
            if (prefMap[key] is double num4)
            {
                return (float)EnsureValid(key, (float)num4);
            }
        }
        if (OptionsManager.defaultOptions.ContainsKey(option))
        {
            object obj = OptionsManager.defaultOptions[option];
            if (obj is float)
            {
                return (float)obj;
            }
        }
        return fallback;
    }

    //public void SetFloatLocal(string key, float content)
    //{
    //    EnsureInitialized();
    //    content = (float)EnsureValid(key, content);
    //    if (localPrefMap.ContainsKey(key))
    //    {
    //        localPrefMap[key] = content;
    //    }
    //    else
    //    {
    //        localPrefMap.Add(key, content);
    //    }
    //    if (CommitMode == PrefsCommitMode.Immediate)
    //    {
    //        CommitPrefs(local: true);
    //    }
    //    if (CommitMode == PrefsCommitMode.DirtySlowTick)
    //    {
    //        isLocalDirty = true;
    //    }
    //    onPrefChanged?.Invoke(option, content);
    //}

    public void SetFloat(OptionType option, float content)
    {
        EnsureInitialized();
        string key = option.ToString();
        content = (float)EnsureValid(key, content);
        if (prefMap.ContainsKey(key))
        {
            prefMap[key] = content;
        }
        else
        {
            prefMap.Add(key, content);
        }
        if (CommitMode == PrefsCommitMode.Immediate)
        {
            CommitPrefs(local: false);
        }
        if (CommitMode == PrefsCommitMode.DirtySlowTick)
        {
            isDirty = true;
        }
        onPrefChanged?.Invoke(option, content);
    }

    public string GetStringLocal(OptionType option, string fallback = null)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (localPrefMap.ContainsKey(key) && localPrefMap[key] is string value)
        {
            return EnsureValid(key, value) as string;
        }
        if (OptionsManager.defaultOptions.ContainsKey(option) && OptionsManager.defaultOptions[option] is string result)
        {
            return result;
        }
        return fallback;
    }

    public string GetString(OptionType option, string fallback = null)
    {
        EnsureInitialized();
        string key = option.ToString();
        if (prefMap.ContainsKey(key) && prefMap[key] is string value)
        {
            return EnsureValid(key, value) as string;
        }
        if (OptionsManager.defaultOptions.ContainsKey(option) && OptionsManager.defaultOptions[option] is string result)
        {
            return result;
        }
        return fallback;
    }

    //public void SetStringLocal(string key, string content)
    //{
    //    EnsureInitialized();
    //    content = EnsureValid(key, content) as string;
    //    if (localPrefMap.ContainsKey(key))
    //    {
    //        localPrefMap[key] = content;
    //    }
    //    else
    //    {
    //        localPrefMap.Add(key, content);
    //    }
    //    if (CommitMode == PrefsCommitMode.Immediate)
    //    {
    //        CommitPrefs(local: true);
    //    }
    //    if (CommitMode == PrefsCommitMode.DirtySlowTick)
    //    {
    //        isLocalDirty = true;
    //    }
    //    onPrefChanged?.Invoke(key, content);
    //}

    public void SetString(OptionType option, string content)
    {
        EnsureInitialized();
        string key = option.ToString();
        content = EnsureValid(key, content) as string;
        if (prefMap.ContainsKey(key))
        {
            prefMap[key] = content;
        }
        else
        {
            prefMap.Add(key, content);
        }
        if (CommitMode == PrefsCommitMode.Immediate)
        {
            CommitPrefs(local: false);
        }
        if (CommitMode == PrefsCommitMode.DirtySlowTick)
        {
            isDirty = true;
        }
        onPrefChanged?.Invoke(option, content);
    }

    private void CommitPrefs(bool local)
    {
        if (local)
        {
            string value = JsonConvert.SerializeObject(localPrefMap, Formatting.Indented);
            localPrefsStream.SetLength(0L);
            StreamWriter streamWriter = new StreamWriter(localPrefsStream);
            streamWriter.Write(value);
            streamWriter.Flush();
        }
        else
        {
            string value2 = JsonConvert.SerializeObject(prefMap, Formatting.Indented);
            prefsStream.SetLength(0L);
            StreamWriter streamWriter2 = new StreamWriter(prefsStream);
            streamWriter2.Write(value2);
            streamWriter2.Flush();
        }
    }

    private void UpdateTimestamp()
    {
        EnsureInitialized();
        DateTime now = DateTime.Now;
        if (!prefMap.ContainsKey("lastTimePlayed.year"))
        {
            prefMap.Add("lastTimePlayed.year", now.Year);
            isDirty = true;
        }
        if (!prefMap.ContainsKey("lastTimePlayed.month"))
        {
            prefMap.Add("lastTimePlayed.month", now.Month);
            isDirty = true;
        }
        if (prefMap["lastTimePlayed.year"] is int num)
        {
            if (num != now.Year)
            {
                prefMap["lastTimePlayed.year"] = now.Year;
                prefMap["lastTimePlayed.month"] = now.Month;
                isDirty = true;
            }
            if (prefMap["lastTimePlayed.month"] is int num2 && num2 != now.Month)
            {
                prefMap["lastTimePlayed.month"] = now.Month;
                isDirty = true;
            }
        }
        else
        {
            prefMap["lastTimePlayed.year"] = now.Year;
            prefMap["lastTimePlayed.month"] = now.Month;
            isDirty = true;
        }
    }

    private void EnsureInitialized()
    {
        if (prefMap == null || localPrefMap == null)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (!Directory.Exists(PrefsPath))
        {
            Directory.CreateDirectory(PrefsPath);
        }
        timeSinceLastTick = 0f;

        // Ensure proper file handling with 'using' statements
        if (prefsStream == null)
        {
            // Use 'FileShare.ReadWrite' to allow other processes to also access the file
            prefsStream = new FileStream(Path.Combine(PrefsPath, "Prefs.json"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        if (localPrefsStream == null)
        {
            localPrefsStream = new FileStream(Path.Combine(PrefsPath, "LocalPrefs.json"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        // Loading prefs
        prefMap = LoadPrefs(prefsStream);
        localPrefMap = LoadPrefs(localPrefsStream);

        // Check if NOTE.txt exists, if not create it
        if (!File.Exists(Path.Combine(PrefsPath, "NOTE.txt")))
        {
            File.WriteAllText(Path.Combine(PrefsPath, "NOTE.txt"), prefsNote);
            Debug.LogWarning("NOTE.txt created in prefs folder. Please read it.");
        }

        Debug.Log("PrefsManager initialized");
    }

    private object EnsureValid(string key, object value)
    {
        if (!propertyValidators.ContainsKey(key))
        {
            return value;
        }
        return propertyValidators[key](value) ?? value;
    }

    private Dictionary<string, object> LoadPrefs(FileStream stream)
    {
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(new StreamReader(stream).ReadToEnd()) ?? new Dictionary<string, object>();
    }

    protected override void Awake()
    {
        base.Awake();

        Initialize();

        if (PrefsManager.Instance == this)
        {
            int year = GetInt("lastTimePlayed.year", -1);
            int month = GetInt("lastTimePlayed.month", -1);
            if (year == -1 || month == -1)
            {
                monthsSinceLastPlayed = 0;
                return;
            }
            DateTime now = DateTime.Now;
            monthsSinceLastPlayed = (now.Year - year) * 12 + now.Month - month;
        }
    }

    private void Start()
    {
        LoadDefaultOptionsPrefs();
        UpdateTimestamp();
    }

    public void LoadDefaultOptionsPrefs()
    {
        EnsureInitialized();
        foreach (var kvp in OptionsManager.defaultOptions)
        {
            string key = kvp.Key.ToString();
            if (kvp.Value is int)
            {
                int value = GetInt(kvp.Key);
                onPrefChanged?.Invoke(kvp.Key, value);
            }
            else if (kvp.Value is float)
            {
                float value = GetFloat(kvp.Key);
                onPrefChanged?.Invoke(kvp.Key, value);
            }
            else if (kvp.Value is bool)
            {
                onPrefChanged?.Invoke(kvp.Key, GetBool(kvp.Key));
            }
            else if (kvp.Value is string)
            {
                onPrefChanged?.Invoke(kvp.Key, GetString(kvp.Key));
            }
            else
            {

                onPrefChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }
    }

    private void FixedUpdate()
    {
        if ((isDirty || isLocalDirty) && CommitMode == PrefsCommitMode.DirtySlowTick && (float)timeSinceLastTick >= 3f)
        {
            timeSinceLastTick = 0f;
            if (isLocalDirty)
            {
                CommitPrefs(local: true);
            }
            if (isDirty)
            {
                CommitPrefs(local: false);
            }
            isLocalDirty = false;
            isDirty = false;
        }
    }

    private void OnApplicationQuit()
    {
        UpdateTimestamp();
        if (CommitMode == PrefsCommitMode.OnQuit || CommitMode == PrefsCommitMode.DirtySlowTick)
        {
            CommitPrefs(local: false);
            CommitPrefs(local: true);
        }
        prefsStream?.Close();
        localPrefsStream?.Close();
    }
}