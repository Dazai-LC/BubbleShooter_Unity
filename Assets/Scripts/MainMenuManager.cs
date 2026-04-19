using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc phải có để chuyển cảnh

public class MainMenuManager : MonoBehaviour
{
    // Hàm này sẽ gọi khi bấm nút Play
    public void PlayGame()
    {
        // Nó sẽ load Scene tiếp theo trong danh sách Build (thường là màn chơi chính)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Hàm này sẽ gọi khi bấm nút Quit
    public void QuitGame()
    {
        Debug.Log("Đã thoát game!"); // Hiện ở bảng Console để mình biết nó chạy
        Application.Quit(); // Lệnh thoát ứng dụng thực tế khi build ra máy
    }
}