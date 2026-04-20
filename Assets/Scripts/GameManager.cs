using UnityEngine;
using TMPro; // Để điều khiển chữ trên màn hình
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Tạo một bản duy nhất (Instance) để mọi chỗ khác gọi được
    public static GameManager Instance;

    [Header("UI Setup")]
    public GameObject endGamePanel; // Cái bảng sẽ hiện lên khi thua
    public TextMeshProUGUI resultText; // Chữ hiện "Game Over" hay "Victory"

    private void Awake()
    {
        // Thiết lập Singleton
        if (Instance == null) Instance = this;
    }

    // ĐÂY LÀ HÀM MÀ DAZAI ĐANG TÌM KIẾM!
    public void GameOver()
    {
        Debug.Log("Dazai ơi, thua rồi!");
        endGamePanel.SetActive(true); // Hiện cái bảng thông báo lên
        //resultText.text = "THUA RỒI NHA!";
        //resultText.color = Color.red;

        Time.timeScale = 0f; // Dừng toàn bộ game lại (bóng ngừng bay)
    }

    // Hàm để bấm nút chơi lại
    public void RestartGame()
    {
        Time.timeScale = 1f; // Chạy lại thời gian
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Nhớ kiểm tra xem đã có dòng này ở trên cùng chưa: 
    // using UnityEngine.SceneManagement;

    public void GoToMenu()
    {
        Time.timeScale = 1f; // Phải trả thời gian về bình thường trước khi chuyển cảnh
                             // Load Scene Menu. Dazai kiểm tra xem Scene Menu của fen tên là gì (thường là "MainMenu")
        SceneManager.LoadScene("MainMenu");
    }
}