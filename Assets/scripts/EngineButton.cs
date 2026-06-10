using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Обрабатывает нажатие XR-кнопки и передаёт его в систему запуска двигателя
// DEPRECATED
public class EngineButton : MonoBehaviour
{
    // XR-компонент, который позволяет взаимодействовать с объектом
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

    // Ссылка на менеджер последовательности запуска двигателя
    private EngineStartSequence manager;

    // Вызывается Unity при создании объекта
    private void Awake()
    {
        // GetComponent<T>()
        // Получает компонент указанного типа с текущего объекта
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        // FindObjectOfType<T>()
        // Ищет первый объект указанного типа на сцене
        manager = FindObjectOfType<EngineStartSequence>();
    }

    // Вызывается при включении объекта
    private void OnEnable()
    {
        // selectEntered
        // Событие XR Interaction Toolkit, возникающее при взаимодействии

        // AddListener()
        // Подписывает метод на событие
        interactable.selectEntered.AddListener(OnPressed);
    }

    // Вызывается при выключении объекта
    private void OnDisable()
    {
        // RemoveListener()
        // Удаляет подписку на событие
		//Чтобы избежать лишних вызовов и утечек ссылок после отключения объекта.
        interactable.selectEntered.RemoveListener(OnPressed);
    }

    // Вызывается при нажатии или выборе объекта через XR Interaction Toolkit
    private void OnPressed(SelectEnterEventArgs args)
    {
        // gameObject
        // Ссылка на объект, на котором висит данный скрипт

        // name
        // Имя объекта в сцене

        // Передаёт имя нажатой кнопки в менеджер запуска двигателя
        manager.RegisterButtonPress(gameObject.name);
    }
}