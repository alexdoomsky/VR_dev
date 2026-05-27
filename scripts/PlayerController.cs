using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class PlayerController : MonoBehaviour
{
    public InputActionReference LeftTriggerValue;
    public InputActionReference RightTriggerValue;

    [Header("Body components")]
    public Transform LeftHand;
    public Transform RightHand;
    public Transform Head;

    private float _leftValue;
    private float _rightValue;

    private float _logTimer = 0f;
    private const float LOG_INTERVAL = 0.1f; // чтобы не заспамить всё к чертям

    private void Start()
    {
        PlayerLogger.Initialize();
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

    private void Update()
    {
        _leftValue = LeftTriggerValue.action.ReadValue<float>();
        _rightValue = RightTriggerValue.action.ReadValue<float>();

//       _logTimer += Time.deltaTime;
//
//        if (_logTimer >= LOG_INTERVAL)
//        {
//            _logTimer = 0f;
//
//            string logText = $"LT: {_leftValue:F2} | RT: {_rightValue:F2}";
//
//            PlayerLogger.Message(logText);
//        }
    }
}
