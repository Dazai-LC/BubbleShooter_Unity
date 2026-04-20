using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gridBallsText;
    public TextMeshProUGUI ammoText;

    [Tooltip("Kéo chữ ComboText bự giữa màn hình vào đây")]
    public TextMeshProUGUI comboText;

    [Header("Game Stats")]
    public int startingAmmo = 40;
    private int currentScore = 0;
    private int currentAmmo;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentAmmo = startingAmmo;
        AddScore(0);
        UpdateAmmoText();
        if (comboText != null) comboText.gameObject.SetActive(false); // Ẩn combo lúc đầu
    }

    public void AddScore(int points)
    {
        currentScore += points;
        if (scoreText != null) scoreText.text = "Điểm: " + currentScore;
    }

    public void UpdateGridBalls(int count)
    {
        if (gridBallsText != null) gridBallsText.text = "Mục tiêu: " + count;
    }

    public void UseAmmo()
    {
        currentAmmo--;
        UpdateAmmoText();
    }

    private void UpdateAmmoText()
    {
        if (ammoText != null) ammoText.text = "Đạn: " + currentAmmo;
    }

    // ==========================================
    // HỆ THỐNG HIỂN THỊ COMBO
    // ==========================================
    public void ShowComboAction(int chainCombo, int bubblesPopped, int pointsAdded)
    {
        if (comboText == null) return;

        // Hủy lệnh ẩn Text trước đó (nếu người chơi bắn quá nhanh)
        CancelInvoke("HideComboText");

        comboText.gameObject.SetActive(true);
        string message = "";

        // 1. Nếu bắn nổ nhiều quả cùng lúc
        if (bubblesPopped >= 8) message += "<color=#FF00FF>THẦN THÁNH!</color>\n";
        else if (bubblesPopped >= 5) message += "<color=#FF4500>TUYỆT ĐỈNH!</color>\n";

        // 2. Nếu giữ được chuỗi bắn liên tục
        if (chainCombo > 1)
        {
            message += $"<color=yellow>Liên hoàn x{chainCombo}</color>\n";
        }

        // 3. Hiện điểm cộng
        message += $"<color=white>+{pointsAdded}</color>";

        comboText.text = message;

        // Tự động tắt sau 1.5 giây
        Invoke("HideComboText", 1.5f);
    }

    private void HideComboText()
    {
        if (comboText != null) comboText.gameObject.SetActive(false);
    }
}