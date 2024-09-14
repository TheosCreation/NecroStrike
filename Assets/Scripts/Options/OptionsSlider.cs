using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsSlider : OptionsBase
{
    private Slider slider;
    private TMP_Text text;
    private Button resetButton;
    private Action<FloatSetting> updateValueAction;
    private FloatSetting floatSetting;
    private bool isPercentage;

    public OptionsSlider(Slider slider, TMP_Text text, Button resetButton, Action<FloatSetting> updateValueAction, FloatSetting floatSetting, bool isPercentage)
    {
        this.slider = slider;
        this.text = text;
        this.resetButton = resetButton;
        this.updateValueAction = updateValueAction;
        this.floatSetting = floatSetting;
        this.isPercentage = isPercentage;
    }

    public override void Initialize()
    {
        if (slider != null)
        {
            float value = GetPlayerPrefValue();
            slider.value = Convert.ToSingle(value);
            UpdateText(value);
            updateValueAction(floatSetting);

            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if(resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefault);
        }
    }

    public override void CleanUp()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetToDefault);
        }
    }

    public override void Update()
    {
        updateValueAction(floatSetting);
    }
    private void OnSliderValueChanged(float value)
    {
        UpdateText(value);
        SaveValue(value);
        updateValueAction(floatSetting);
    }

    private void UpdateText(float value)
    {
        string settingsText = value.ToString();
        if(isPercentage)
        {
            settingsText += "%";
        }
        text.text = settingsText;
    }

    private void SaveValue(float value)
    {
        PlayerPrefs.SetFloat(floatSetting.name, Convert.ToSingle(value));

        // Add more types as needed
        PlayerPrefs.Save();
    }

    private float GetPlayerPrefValue()
    {
        return PlayerPrefs.GetFloat(floatSetting.name, Convert.ToSingle(floatSetting.defaultValue));
    }

    public void ResetToDefault()
    {
        slider.value = Convert.ToSingle(floatSetting.defaultValue);
        UpdateText(floatSetting.defaultValue);
        SaveValue(floatSetting.defaultValue);
        updateValueAction(floatSetting);
    }
}
