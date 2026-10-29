using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Tooltip("Порядок в списке = порядок переключения (F1, F2, F3, F4)")]
    [SerializeField] MonoBehaviour[] viewBehaviours;
    [SerializeField] int startIndex = 0;
    [SerializeField] KeyCode nextKey = KeyCode.V;
    [SerializeField] bool showLabel = true;

    readonly List<ICameraView> views = new();
    int current = -1;

    public ICameraView Current => current >= 0 ? views[current] : null;
    public event Action<ICameraView> Changed;

    void Awake()
    {
        foreach (var b in viewBehaviours)
        {
            if (b is ICameraView v) views.Add(v);
            else Debug.LogWarning($"{(b ? b.name : "пустой элемент")} не реализует ICameraView", this);
        }
    }

    void Start()
    {
        // Сначала гасим все виды, потом включаем стартовый
        foreach (var v in views)
        {
            SetCamera(v, false);
            v.OnExit();
        }
        Switch(startIndex);
    }

    void Update()
    {
        if (Input.GetKeyDown(nextKey)) Next();

        for (int i = 0; i < views.Count && i < 4; i++)
            if (Input.GetKeyDown(KeyCode.F1 + i)) Switch(i);
    }

    public void Next() => Switch(current + 1);

    public void Switch(int index)
    {
        if (views.Count == 0) return;
        index = ((index % views.Count) + views.Count) % views.Count;
        if (index == current) return;

        if (current >= 0)
        {
            views[current].OnExit();
            SetCamera(views[current], false);
        }

        current = index;
        SetCamera(views[current], true);
        views[current].OnEnter();
        Changed?.Invoke(views[current]);
    }

    // Камера и AudioListener включаются вместе, иначе будет предупреждение о двух слушателях
    static void SetCamera(ICameraView v, bool on)
    {
        var cam = v.ViewCamera;
        if (!cam) return;
        cam.enabled = on;
        var listener = cam.GetComponent<AudioListener>();
        if (listener) listener.enabled = on;
    }

    void OnGUI()
    {
        if (!showLabel || Current == null) return;
        GUI.Label(new Rect(20, 20, 500, 25), $"Вид: {Current.ViewName}   [V - сменить, F1-F4]");
    }
}