using Mirror;
using UnityEngine;

namespace Smooth
{
    public class SmoothControllerMirror : MonoBehaviour
    {
        public static bool isHandlerRegistered = false;

        private void Update()
        {
            if (NetworkServer.active || NetworkClient.active)
                isHandlerRegistered = true;
            else
                isHandlerRegistered = false;
        }

        public static void RegisterHandlers()
        {
            // SmoothSyncMirror регистрирует хэндлеры сам в OnStartServer и OnStartClient.
            // Делать это снаружи не нужно и вызывает конфликт типов в новых версиях Mirror.
            isHandlerRegistered = true;
        }
    }
}