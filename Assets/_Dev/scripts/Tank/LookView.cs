using UnityEngine;

public class LookView : MonoBehaviour, ILookView
{
    [SerializeField] string viewName = "Вид";
    [SerializeField] Camera cam;
    [SerializeField] Transform root;                    // что вращаем; позиция = место камеры на танке
    [SerializeField] Vector2 fovRange = new(20f, 70f);  // (макс. приближение, макс. обзор)
    [SerializeField] float zoomStep = 5f;
    [SerializeField] bool slowerWhenZoomed = true;      // при зуме мышь медленнее

    public string ViewName => viewName;
    public Camera ViewCamera => cam;
    public float SensitivityScale => slowerWhenZoomed ? cam.fieldOfView / fovRange.y : 1f;

    void Awake()
    {
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, fovRange.x, fovRange.y);
    }

    public void OnEnter() { }
    public void OnExit() { }

    public void ApplyLook(Quaternion worldRotation)
    {
        root.rotation = worldRotation;
    }

    public void Zoom(float scroll)
    {
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomStep, fovRange.x, fovRange.y);
    }
}