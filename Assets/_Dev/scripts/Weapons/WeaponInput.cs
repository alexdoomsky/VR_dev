using UnityEngine;

public class WeaponInput : MonoBehaviour
{
    IGun gun;
    bool locked = true;

    void Awake()
    {
        gun = GetComponentInParent<IGun>();
        if (gun == null) gun = GetComponentInChildren<IGun>();
        if (gun == null)
            Debug.LogError("WeaponInput: не найден GunController. Повесь WeaponInput на Tank рядом с GunController.", this);
    }

    void Start() => SetCursor(true);

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) SetCursor(locked);
    }

    void OnDestroy()
    {
        // чтобы в меню курсор был виден
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void SetCursor(bool lockIt)
    {
        locked = lockIt;
        Cursor.lockState = lockIt ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockIt;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursor(false);
            return;
        }

        // Курсор свободен: первый клик возвращает захват и не стреляет
        if (!locked)
        {
            if (Input.GetMouseButtonDown(0)) SetCursor(true);
            return;
        }

        if (gun == null) return;

        if (Input.GetMouseButtonDown(0)) gun.TryFire();
        if (Input.GetKeyDown(KeyCode.R)) gun.StartLoading();
        if (Input.GetKeyDown(KeyCode.Alpha1)) gun.SelectShell(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) gun.SelectShell(1);

        if (Input.GetKeyDown(KeyCode.F10)) BackToMenu.Go();
    }

    void OnGUI()
    {
        if (gun == null) return;
        GUI.Label(new Rect(20, 45, 500, 25),
            $"Пушка: {gun.State}  {gun.LoadProgress:P0}  снаряд №{gun.SelectedShell + 1}" +
            (locked ? "" : "   [клик - вернуть мышь]"));
    }
}