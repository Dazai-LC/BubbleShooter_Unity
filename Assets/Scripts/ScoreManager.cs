using UnityEngine;
using TMPro; // Nếu fen dùng TextMeshPro

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TextMeshProUGUI scoreText;
    private int currentScore = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + currentScore.ToString();
    }
}