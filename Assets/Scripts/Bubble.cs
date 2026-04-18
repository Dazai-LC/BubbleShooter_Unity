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
        Debug.Log($"<color=yellow>Hàm Drop đã chạy cho quả bóng: {gameObject.name}</color>");

        IsSnapped = false;

        // Đổi Layer
        int fallingLayer = LayerMask.NameToLayer("FallingBubble");
        if (fallingLayer != -1)
        {
            gameObject.layer = fallingLayer;
            Debug.Log($"Da doi Layer sang ID: {fallingLayer}");
        }
        else
        {
            Debug.LogError("KHÔNG TÌM THẤY Layer 'FallingBubble' trong Tags and Layers!");
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1.5f;

        Destroy(gameObject, 2f);
    }

    public void Pop()
    {
        // QUAN TRỌNG: Tắt ngay va chạm để thuật toán rụng bóng không quét nhầm vào "xác" của quả bóng này
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