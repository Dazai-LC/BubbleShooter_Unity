using UnityEngine;

public enum BubbleColor { Green, Red, Yellow }

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Bubble : MonoBehaviour
{
    [field: SerializeField] public BubbleColor Color { get; private set; }

    [Header("VFX")]
    [SerializeField] private GameObject popEffectPrefab;

    public bool IsMoving { get; private set; } = false;
    public bool IsSnapped { get; set; } = false;

    private Rigidbody2D rb;
    private Collider2D col; // Cache lại collider

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
    }

    public void Fire(Vector2 direction, float speed)
    {
        IsMoving = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = direction * speed;
    }

    public void StopAndSnap()
    {
        IsMoving = false;
        IsSnapped = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void Drop()
    {
        // Thưởng 200 điểm cho mỗi quả bóng rụng (mồ côi)
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(200);

        Debug.Log($"<color=yellow>Hàm Drop đã chạy cho quả bóng: {gameObject.name}</color>");
        IsSnapped = false;

        int fallingLayer = LayerMask.NameToLayer("FallingBubble");
        if (fallingLayer != -1)
        {
            gameObject.layer = fallingLayer;
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1.5f;
        Destroy(gameObject, 2f);
    }

    // Thêm tham số multiplier để nhân điểm khi nổ combo
    public void Pop(int multiplier = 1)
    {
        // Điểm = 100 x hệ số combo
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(100 * multiplier);

        col.enabled = false;

        if (popEffectPrefab != null)
        {
            Instantiate(popEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsMoving) return;

        if (collision.gameObject.CompareTag("Bubble") || collision.gameObject.CompareTag("TopWall"))
        {
            StopAndSnap();
            BubbleManager.Instance.ProcessSnappedBubble(this);
        }
    }
}