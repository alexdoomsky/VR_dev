using UnityEngine;
using UnityEngine.InputSystem;

public class ClutchController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference clutchAction;

    [Header("Smoothing")]
    [SerializeField] private float responseSpeed = 8f;

    [Header("Debug inversion")]
    [SerializeField] private bool invertInput = false;

    [Header("Output")]
    [Range(0f, 1f)]
    public float ClutchValue;
    // 0 = отпущено (полная связь)
    // 1 = выжато (разрыв)

    private float rawValue;

    private void OnEnable()
    {
        clutchAction?.action.Enable();
    }

    private void OnDisable()
    {
        clutchAction?.action.Disable();
    }

    private void Update()
    {
        if (clutchAction == null) return;

        rawValue = clutchAction.action.ReadValue<float>();

        if (invertInput)
            rawValue = 1f - rawValue;

        ClutchValue = Mathf.Lerp(
            ClutchValue,
            rawValue,
            Time.deltaTime * responseSpeed
        );
    }

    /// <summary>
    /// Насколько сцепление передаёт момент
    /// 1 = полностью сцеплено
    /// 0 = полностью выжато
    /// </summary>
    public float GetCoupling()
    {
        return 1f - ClutchValue;
    }

    /// <summary>
    /// Можно использовать для КПП / двигателя
    /// </summary>
    public bool IsDisengaged(float threshold = 0.9f)
    {
        return ClutchValue >= threshold;
    }
}

