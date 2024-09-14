using UnityEngine;
using UnityEngine.UI;

public class DynamicVerticalSpacing : MonoBehaviour
{
    private VerticalLayoutGroup verticalLayoutGroup;
    private RectTransform parentRect;
    [SerializeField] private float spacingScale = 0.05f;
    private void Start()
    {
        parentRect = transform.parent.GetComponent<RectTransform>();
        verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
    }

    void Update()
    {
        float parentHeight = parentRect.rect.height;
        verticalLayoutGroup.spacing = parentHeight * spacingScale;
    }
}