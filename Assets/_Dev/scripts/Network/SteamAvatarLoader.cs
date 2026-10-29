using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public static class SteamAvatarLoader
{
    static readonly Dictionary<CSteamID, Texture2D> cache = new();

    public static void Load(CSteamID id, Action<Texture2D> cb)
    {
        if (cache.TryGetValue(id, out var t)) { cb(t); return; }

        int handle = SteamFriends.GetMediumFriendAvatar(id);
        if (handle == 0) return;

        uint w, h;
        SteamUtils.GetImageSize(handle, out w, out h);
        var buf = new byte[w * h * 4];
        SteamUtils.GetImageRGBA(handle, buf, buf.Length);

        var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
        // Steam даёт перевёрнутое изображение
        var flipped = new byte[w * h * 4];
        for (int row = 0; row < h; row++)
            Array.Copy(buf, (int)((h - 1 - row) * w * 4), flipped, (int)(row * w * 4), (int)(w * 4));
        tex.LoadRawTextureData(flipped);
        tex.Apply();
        cache[id] = tex;
        cb(tex);
    }
}