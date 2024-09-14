using UnityEngine;

public class Scope : MonoBehaviour
{
    [SerializeField] private float cameraFov = 30f;
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
        scopeCamera.transform.parent.gameObject.SetActive(false);
        scopeCamera.fieldOfView = cameraFov;
    }

    public void SetZoom(bool zoom, PlayerLook playerLook)
    {
        if (zoom)
        {
            scopeCamera.transform.parent.gameObject.SetActive(true);
            playerLook.sensitivityMultiplier = cameraFov / 30;

            if (cameraFov / 30 > 1)
            {
                Debug.LogAssertion("Please fix this code or scope");
            }
        }
        else
        {
            scopeCamera.transform.parent.gameObject.SetActive(false);
            playerLook.sensitivityMultiplier = 1;
        }
    }
}
