using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryButton : MonoBehaviour
{
    [HideInInspector] public Button button;
    [SerializeField] private TMP_Text weaponText;

    private void Awake()
    {
        button = GetComponent<Button>();
    }
    public void SetText(string text)
    {
        weaponText.text = text;
    }
}