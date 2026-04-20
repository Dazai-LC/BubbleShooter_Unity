using UnityEngine;
using UnityEngine.SceneManagement;

public class WinUIHandler : MonoBehaviour
{
    // 1. Nút TIẾP THEO
    public void OnNextLevelClick()
    {
        int currentLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        PlayerPrefs.SetInt("SelectedLevel", currentLevel + 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 2. Nút CHƠI LẠI (Back/Restart)
    public void OnRestartClick()
    {
        // Load lại chính nó mà không tăng Level
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 3. Nút MENU LEVEL
    public void OnMenuLevelClick()
    {
        SceneManager.LoadScene("MenuLevel");
    }
}