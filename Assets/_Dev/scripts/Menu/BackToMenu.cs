using UnityEngine.SceneManagement;

public static class BackToMenu
{
    public static void Go(string menuScene = "MainMenu")
    {
        // Сессия сама остановит Mirror, выйдет из Steam-лобби и загрузит меню
        if (SessionService.I) SessionService.I.LeaveSession();
        else SceneManager.LoadScene(menuScene);
    }
}
