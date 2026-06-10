using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
//DEBUG
//DEPRECATED

// NotepadLogScroll — UI компонент для отображения логов в виде списка (scrollable feed)
// Используется вместе с PlayerLogger.NotepadLogEvent
// Реализует простую очередь сообщений с ограничением количества элементов
public class NotepadLogScroll : MonoBehaviour
{
    // const = compile-time константа
    // TEXT_LIMIT — максимальное количество отображаемых логов
    // при превышении старые элементы удаляются (FIFO очередь)
    const int TEXT_LIMIT = 50;

    [SerializeField] private GameObject _logTextPrefab;
    // prefab UI элемента текста (обычно TMP_Text внутри UI объекта)
    // используется как шаблон для каждого нового лог-сообщения

    [SerializeField] private Transform _logContentTransform;
    // родительский UI контейнер (например Vertical Layout Group в ScrollView)
    // все новые элементы логов добавляются сюда

    // список всех созданных UI элементов логов
    // используется для контроля лимита и удаления старых элементов
    private List<GameObject> _logTexts;

    // Start вызывается один раз при старте сцены
    private void Start()
    {
        // инициализация списка
        _logTexts = new List<GameObject>();

        // подписка на event логгера
        // PlayerLogger.NotepadLogEvent:
        // UnityEvent<string> → вызывает CreateEntry при каждом новом лог-сообщении
        PlayerLogger.NotepadLogEvent.AddListener(CreateEntry);
    }

    // CreateEntry — callback метод для обработки нового лог-сообщения
    // вызывается каждый раз, когда PlayerLogger генерирует новый log message
    //
    // text:
    // строка сообщения (уже отформатированная, без timestamp)
    private void CreateEntry(string text)
    {
        // Instantiate:
        // создаёт копию prefab в сцене
        GameObject go = Instantiate(_logTextPrefab);

        // GetComponent<RectTransform>()
        // UI элементы в Unity используют RectTransform вместо Transform
        // SetParent(..., false):
        // false = сохранить локальные координаты prefab (не пересчитывать мировые)
        go.GetComponent<RectTransform>().SetParent(_logContentTransform, false);

        // TMP_Text — базовый компонент TextMeshPro (UI текст)
        // устанавливаем текстовое содержимое лог строки
        go.GetComponent<TMP_Text>().text = text;

        // сохраняем ссылку на созданный объект для управления памятью/UI
        _logTexts.Add(go);

        // контроль размера списка логов
        // FIFO (First In First Out):
        // если элементов больше TEXT_LIMIT — удаляем самый старый
        if (_logTexts.Count > TEXT_LIMIT)
        {
            // Destroy:
            // удаляет GameObject из сцены (через один кадр Unity)
            Destroy(_logTexts[0]);

            // удаляем ссылку из списка
            _logTexts.RemoveAt(0);
        }
    }
}