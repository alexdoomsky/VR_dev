using UnityEngine.SceneManagement;

public static class BackToMenu
{
    public static void Go(string menuScene = "MainMenu")
    {
        // XR не трогаем: меню само решит, нужен он или нет
        SceneManager.LoadScene(menuScene);
    }
}