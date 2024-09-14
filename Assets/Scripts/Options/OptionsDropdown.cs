using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsDropdown : OptionsBase
{
    private TMP_Dropdown dropdown;
    private Button resetButton;
    private Action<IntSetting> updateValueAction;
    private IntSetting intSetting;

    public OptionsDropdown(TMP_Dropdown dropdown, Button resetButton, Action<IntSetting> updateValueAction, IntSetting intSetting)
    {
        this.dropdown = dropdown;
        this.resetButton = resetButton;
        this.updateValueAction = updateValueAction;
        this.intSetting = intSetting;
    }

    public override void Initialize()
    {
        if (dropdown != null)
        {
            int value = GetPlayerPrefValue();
            dropdown.value = value;
            updateValueAction(intSetting);

            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefault);
        }
    }

    public override void CleanUp()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetToDefault);
        }
    }
    public override void Update()
    {
        updateValueAction(intSetting);
    }
    private void OnDropdownValueChanged(int value)
    {
        SaveValue(value);
        updateValueAction(intSetting);
    }

    private void SaveValue(int value)
    {
        PlayerPrefs.SetInt(intSetting.name, value);
        PlayerPrefs.Save();
    }

    private int GetPlayerPrefValue()
    {
        return PlayerPrefs.GetInt(intSetting.name, intSetting.defaultValue);
    }

    public void ResetToDefault()
    {
        dropdown.value = intSetting.defaultValue;
        SaveValue(intSetting.defaultValue);
        updateValueAction(intSetting);
    }
}
