using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class UiBar : MonoBehaviour
{
    private Slider barSlider;
    private void Awake()
    {
        barSlider = GetComponent<Slider>();
    }
    public void UpdateBar(float percentage)
    {
        barSlider.value = percentage;
    }
}