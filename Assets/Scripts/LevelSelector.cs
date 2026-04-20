using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelSelector : MonoBehaviour
{
    public int levelIndex; // Số màn chơi (Fen điền 1, 2, 3... trong Inspector)
    public TextMeshProUGUI levelText;

    private void Start()
    {
        // Tự động lấy component Text nếu fen chưa kéo vào
        if (levelText == null)
            levelText = GetComponentInChildren<TextMeshProUGUI>();

        // Tự động hiển thị số Level lên nút bấm ngay khi vào Menu
        if (levelText != null)
            levelText.text = levelIndex.ToString();
    }

    // Hàm này dùng nếu fen muốn tạo nút bằng code (hiện tại chưa dùng tới)
    public void SetupButton(int index)
    {
        levelIndex = index;
        if (levelText != null) levelText.text = levelIndex.ToString();
    }

    public void OnLevelButtonClick()
    {
        // Lưu lại số 1, 2 hoặc 3...
        PlayerPrefs.SetInt("SelectedLevel", levelIndex);

        // Chuyển cảnh - Nhớ Check lại tên Scene trong Build Settings nhé!
        SceneManager.LoadScene("GameScene");
    }
}