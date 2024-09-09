using UnityEngine;

public class Scope : MonoBehaviour
{
    [SerializeField] private Crosshair crosshairPrefab;
    [SerializeField] private Transform crosshairAttachmentTransform;
    public GameObject zoomGlass; 
    public float zoomAmount = 1.1f;

    private void Awake()
    {
        Crosshair crosshair = Instantiate(crosshairPrefab);

        crosshair.transform.parent = crosshairAttachmentTransform;
        crosshair.transform.localEulerAngles = Vector3.zero;
        crosshair.transform.localPosition = Vector3.zero;
        crosshair.transform.localScale = new Vector3(1, 1, 1);
    }
}
