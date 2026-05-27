using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NotepadLogScroll : MonoBehaviour
{
    const int TEXT_LIMIT = 50;

    [SerializeField] private GameObject _logTextPrefab;
    [SerializeField] private Transform _logContentTransform;

    private List<GameObject> _logTexts;

    private void Start()
    {
        _logTexts = new List<GameObject>();
        PlayerLogger.NotepadLogEvent.AddListener(CreateEntry);
    }

    private void CreateEntry(string text)
    {
        GameObject go = Instantiate(_logTextPrefab);
        go.GetComponent<RectTransform>().SetParent(_logContentTransform, false);
        go.GetComponent<TMP_Text>().text = text;
        _logTexts.Add(go);

        // if too many texts, delete first ones
        if (_logTexts.Count > TEXT_LIMIT)
        {
            Destroy(_logTexts[0]);
            _logTexts.RemoveAt(0);
        }
    }
}