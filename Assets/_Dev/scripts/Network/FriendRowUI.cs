using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class FriendRowUI : MonoBehaviour
{
    [SerializeField] RawImage avatar;
    [SerializeField] TMP_Text lblName;
    [SerializeField] Button btnInvite;

    public void Set(CSteamID id, Action onInvite)
    {
        lblName.text = SteamFriends.GetFriendPersonaName(id);
        btnInvite.onClick.AddListener(() => onInvite());
        SteamAvatarLoader.Load(id, tex => { if (avatar) avatar.texture = tex; });
    }
}