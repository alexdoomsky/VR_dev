using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class PlayerSlotUI : MonoBehaviour
{
    [SerializeField] RawImage avatar;
    [SerializeField] TMP_Text lblName;
    [SerializeField] TMP_Text lblPlatform;
    [SerializeField] TMP_Text lblReady;
    [SerializeField] GameObject emptyPanel;

    public void SetPlayer(CSteamID id, string platform, bool ready)
    {
        bool has = id != CSteamID.Nil && id.IsValid();
        emptyPanel.SetActive(!has);
        avatar.gameObject.SetActive(has);
        lblName.gameObject.SetActive(has);

        if (!has) return;

        lblName.text = SteamFriends.GetFriendPersonaName(id);
        lblPlatform.text = platform;
        lblReady.text = ready ? "? Готов" : "...";
        lblReady.color = ready ? Color.green : Color.gray;

        SteamAvatarLoader.Load(id, tex => { if (avatar) avatar.texture = tex; });
    }
}