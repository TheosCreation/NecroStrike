using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionsToggle : OptionsBase
{
    private Toggle toggle;
    private Button resetButton;
    private Action<BoolSetting> updateValueAction;
    private BoolSetting boolSetting;

    public OptionsToggle(Toggle toggle, Button resetButton, Action<BoolSetting> updateValueAction, BoolSetting boolSetting)
    {
        this.toggle = toggle;
        this.resetButton = resetButton;
        this.updateValueAction = updateValueAction;
        this.boolSetting = boolSetting;
    }

    public override void Initialize()
    {
        if (toggle != null)
        {
            bool value = GetPlayerPrefValue();
            toggle.isOn = value;
            updateValueAction(boolSetting);

            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefault);
        }
    }

    public override void CleanUp()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetToDefault);
        }
    }
    public override void Update()
    {
        updateValueAction(boolSetting);
    }

    private void OnToggleValueChanged(bool value)
    {
        SaveValue(value);
        updateValueAction(boolSetting);
    }

    private void SaveValue(bool value)
    {
        PlayerPrefs.SetInt(boolSetting.name, value ? 1 : 0);

        // Add more types as needed
        PlayerPrefs.Save();
    }

    private bool GetPlayerPrefValue()
    {
        return PlayerPrefs.GetInt(boolSetting.name, boolSetting.defaultValue ? 1 : 0) == 1;
    }

    public void ResetToDefault()
    {
        if (toggle != null)
        {
            toggle.isOn = boolSetting.defaultValue;
            SaveValue(boolSetting.defaultValue);
            updateValueAction(boolSetting);
        }
    }
}
