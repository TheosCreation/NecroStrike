using UnityEngine;

public class Scope : MonoBehaviour
{
    public float cameraFov = 30f;
    public Camera scopeCamera;
    [SerializeField] private CrosshairSO Crosshair;
    [SerializeField] private Transform crosshairAttachmentTransform;

    private void Awake()
    {
        Crosshair crosshair = Instantiate(Crosshair.prefab);

        crosshair.transform.parent = crosshairAttachmentTransform;
        crosshair.transform.localEulerAngles = Vector3.zero;
        crosshair.transform.localPosition = Vector3.zero;
        crosshair.transform.localScale = new Vector3(1, 1, 1);
    }

    private void Start()
    {
        if (scopeCamera == null) return;

        scopeCamera.transform.parent.gameObject.SetActive(false);
        scopeCamera.fieldOfView = cameraFov;
    }

    public void SetZoom(bool zoom)
    {
        if (scopeCamera == null) return;

        if (zoom)
        {
            scopeCamera.transform.parent.gameObject.SetActive(true);

            if (cameraFov / 30 > 1)
            {
                Debug.LogAssertion("Please fix this code or scope");
            }
        }
        else
        {
            scopeCamera.transform.parent.gameObject.SetActive(false);
        }
    }
}
