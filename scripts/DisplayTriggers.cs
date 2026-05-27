using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DisplayTriggers : MonoBehaviour
{
    public InputActionReference LeftTriggerValue;
    public InputActionReference RightTriggerValue;

    public float updateInterval = 0.1f;

    private TextMeshProUGUI textOutput;

    private float leftValue;
    private float rightValue;

    private WaitForSeconds delayTime;

    private void Awake()
    {
        textOutput = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        LeftTriggerValue.action.Enable();
        RightTriggerValue.action.Enable();
    }

    private void OnDisable()
    {
        LeftTriggerValue.action.Disable();
        RightTriggerValue.action.Disable();
    }

    private void Start()
    {
        delayTime = new WaitForSeconds(updateInterval);
        StartCoroutine(UpdateDisplay());
    }

    private void Update()
    {
        leftValue = LeftTriggerValue.action.ReadValue<float>();
        rightValue = RightTriggerValue.action.ReadValue<float>();
    }

    private IEnumerator UpdateDisplay()
    {
        while (true)
        {
            textOutput.text = $"ACC: {leftValue:F2}\nBRK: {rightValue:F2}";
            yield return delayTime;
        }
    }
}
