using UnityEngine;

public class Scope : MonoBehaviour
{
    public Camera scopeCamera;
    [SerializeField] private Crosshair crosshairPrefab;
    [SerializeField] private Transform crosshairAttachmentTransform;

    private void Awake()
    {
        Crosshair crosshair = Instantiate(crosshairPrefab);

        crosshair.transform.parent = crosshairAttachmentTransform;
        crosshair.transform.localEulerAngles = Vector3.zero;
        crosshair.transform.localPosition = Vector3.zero;
        crosshair.transform.localScale = new Vector3(1, 1, 1);
    }

    private void Start()
    {
        scopeCamera.gameObject.SetActive(false);
    }
}
