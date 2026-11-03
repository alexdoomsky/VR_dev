using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button vrButton;
    [SerializeField] private Button pcButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text status;

    [Header("Лобби")]
    [SerializeField] private LobbyScreen lobbyScreen;
    [SerializeField] private GameObject modePanel;

    [Header("Сцены")]
    [SerializeField] private string vrScene = "Tank_VR";
    [SerializeField] private string pcScene = "Tank_PC";

    [Header("Отладка")]
    [Tooltip("Выключить проверку VR-устройства. Только для тестирования без шлема.")]
    [SerializeField] private bool skipVrCheck = false;

    private IControlProfile vr;
    private IControlProfile pc;
    private bool busy;

    private void Awake()
    {
        vr = new VrProfile(vrScene, skipVrCheck);
        pc = new PcProfile(pcScene);

        vrButton.onClick.AddListener(() => Launch(vr));
        pcButton.onClick.AddListener(() => Launch(pc));
        quitButton.onClick.AddListener(QuitGame);
        if (lobbyScreen != null)
            lobbyScreen.OnReturnToMenu += ReturnToModeSelection;
    }

    private void OnDestroy()
    {
        if (vrButton != null) vrButton.onClick.RemoveAllListeners();
        if (pcButton != null) pcButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();
        if (lobbyScreen != null) lobbyScreen.OnReturnToMenu -= ReturnToModeSelection;
    }

    private void Launch(IControlProfile profile)
    {
        if (busy) return;
        StartCoroutine(LaunchRoutine(profile));
    }

    private IEnumerator LaunchRoutine(IControlProfile profile)
    {
        busy = true;

        if (status != null) status.text = "Подготовка режима...";

        bool prepared = false;
        yield return profile.Prepare(result => prepared = result);

        if (!prepared)
        {
            if (status != null)
                status.text = profile.Mode == ControlMode.VR
                    ? "Шлем не найден. Запусти SteamVR/Oculus или выбери режим ПК."
                    : "Не удалось подготовить режим ПК.";
            busy = false;
            yield break;
        }

        GameSettings.Mode = profile.Mode;

        if (lobbyScreen == null)
        {
            if (status != null) status.text = "Ошибка: LobbyScreen не назначен в Inspector.";
            busy = false;
            yield break;
        }

        if (status != null) status.text = "Открываем лобби...";

        if (modePanel != null) modePanel.SetActive(false);

        lobbyScreen.Open();
        busy = false;
    }

    private void QuitGame() => Application.Quit();

    public void ReturnToModeSelection()
    {
        if (lobbyScreen != null) lobbyScreen.gameObject.SetActive(false);
        if (modePanel != null) modePanel.SetActive(true);
        if (status != null) status.text = "Выберите режим игры";
        busy = false;
    }
}