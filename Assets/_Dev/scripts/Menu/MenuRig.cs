
using System.Collections;
using UnityEngine;

public class MenuRig : MonoBehaviour
{
    [Header("Камеры")]
    [SerializeField] private GameObject xrOrigin;
    [SerializeField] private Camera flatCamera;

    [Header("Меню")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private float menuDistance = 2f;

    private IEnumerator Start()
    {
        yield return null;

        bool isVR = XRManager.IsRunning;

        if (xrOrigin != null)
            xrOrigin.SetActive(isVR);

        if (flatCamera != null)
            flatCamera.gameObject.SetActive(!isVR);

        if (menuCanvas != null)
        {
            if (isVR)
            {
                menuCanvas.renderMode = RenderMode.WorldSpace;
                menuCanvas.worldCamera = null;

                Transform head = Camera.main != null
                    ? Camera.main.transform
                    : null;

                if (head != null)
                {
                    menuCanvas.transform.position =
                        head.position + head.forward * menuDistance;

                    menuCanvas.transform.rotation =
                        Quaternion.LookRotation(
                            menuCanvas.transform.position - head.position
                        );
                }
            }
            else
            {
                menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                menuCanvas.worldCamera = null;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = !isVR;
    }
}
