using UnityEngine;

public class DeadlineHandler : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bubble"))
        {
            // Lấy script Bubble từ quả bóng chạm vào
            Bubble bubbleScript = other.GetComponent<Bubble>();

            // Chỉ Game Over khi quả bóng đó đã dính vào lưới (IsSnapped == true)
            if (bubbleScript != null && bubbleScript.IsSnapped)
            {
                Debug.Log("Dazai: Bóng trên lưới đã chạm vạch!");
                GameManager.Instance.GameOver();
            }
        }
    }
}