using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Обрабатывает нажатие кнопки стартера и запускает двигатель
public class StarterButton : MonoBehaviour
{
    // XR-компонент взаимодействия
    private XRSimpleInteractable interactable;

    [Header("References")]

    // Ссылка на систему двигателя
    [SerializeField] private TankEngine engine;

    // Вызывается Unity при создании объекта
    private void Awake()
    {
        // GetComponent<T>()
        // Получает компонент указанного типа с текущего объекта
        interactable = GetComponent<XRSimpleInteractable>();
    }

    // Вызывается при включении объекта
    private void OnEnable()
    {
        // Подписка на событие нажатия
        interactable.selectEntered.AddListener(OnPressed);
    }

    // Вызывается при выключении объекта
    private void OnDisable()
    {
        // Отписка от события
        interactable.selectEntered.RemoveListener(OnPressed);
    }

    // Вызывается при взаимодействии с кнопкой в VR
    private void OnPressed(SelectEnterEventArgs args)
    {
        // Вывод сообщения в Console
        Debug.Log("starter pressed (XR)");

        // Проверка наличия ссылки на двигатель
        if (engine == null)
        {
            // Вывод ошибки в Console
            Debug.LogError(
                "StarterButton: TankEngine reference is missing"
            );

            return;
        }

        // Запуск двигателя
        engine.StartEngine();
    }

    // ContextMenu добавляет пункт в инспектор Unity
    //
    // ПКМ по компоненту ->
    // PRESS STARTER (DEBUG)
    [ContextMenu("PRESS STARTER (DEBUG)")]

    // Отладочный запуск без VR
    private void DebugPress()
    {
        Debug.Log("starter pressed (context menu)");

        if (engine == null)
        {
            Debug.LogError(
                "StarterButton: TankEngine reference is missing"
            );

            return;
        }

        // Запуск двигателя
        engine.StartEngine();
    }
}