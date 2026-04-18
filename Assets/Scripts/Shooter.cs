using UnityEngine;

public class Shooter : MonoBehaviour
{
    [Header("Shooter References")]
    [SerializeField] private Bubble[] bubblePrefabs;
    [Tooltip("Điểm nằm ở mỏm khẩu pháo (Con của Shooter)")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Điểm chờ bóng tiếp theo (Cùng cấp với Shooter)")]
    [SerializeField] private Transform nextBubblePoint;

    [Header("Settings")]
    [SerializeField] private float fireSpeed = 15f;
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private int maxBounces = 1;

    private Bubble currentBubble;
    private Bubble nextBubble;
    private bool canFire = false;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        PrepareInitialBubbles();
    }

    private void Update()
    {
        HandleAimingAndShooting();

        if (canFire && currentBubble != null)
        {
            currentBubble.transform.position = spawnPoint.position;
        }
    }

    private void HandleAimingAndShooting()
    {
        if (Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved))
        {
            Vector3 inputPos = Input.mousePosition;
            Vector3 targetPos = mainCamera.ScreenToWorldPoint(inputPos);
            targetPos.z = 0f;

            Vector2 lookDirection = (targetPos - transform.position).normalized;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            DrawTrajectory(spawnPoint.position, transform.up);
        }

        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            if (canFire && currentBubble != null)
            {
                Shoot(transform.up);
            }
            else
            {
                trajectoryLine.positionCount = 0;
            }
        }
    }

    private void DrawTrajectory(Vector2 startPos, Vector2 direction)
    {
        trajectoryLine.positionCount = 1;
        trajectoryLine.SetPosition(0, startPos);

        Vector2 currentPos = startPos;
        Vector2 currentDir = direction;
        float totalLength = 0f; // Biến để tính tổng độ dài tia

        int mask = LayerMask.GetMask("Wall", "TopWall", "Bubble");

        for (int i = 0; i <= maxBounces; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(currentPos + currentDir * 0.1f, currentDir, 30f, mask);

            Vector2 nextPos;
            if (hit.collider != null)
            {
                nextPos = hit.point;
                totalLength += Vector2.Distance(currentPos, nextPos); // Cộng dồn độ dài đoạn này

                trajectoryLine.positionCount++;
                trajectoryLine.SetPosition(i + 1, nextPos);

                if (hit.collider.CompareTag("Wall"))
                {
                    currentDir = Vector2.Reflect(currentDir, hit.normal);
                    currentPos = nextPos;
                }
                else
                {
                    break;
                }
            }
            else
            {
                nextPos = currentPos + currentDir * 30f;
                totalLength += Vector2.Distance(currentPos, nextPos);

                trajectoryLine.positionCount++;
                trajectoryLine.SetPosition(i + 1, nextPos);
                break;
            }
        }

        // 1. Mật độ chấm: 1 mét có bao nhiêu chấm (vặn cái này để thưa/dày)
        float dotDensity = 1f;

        // 2. Chốt cứng trục Y = 1 để luôn chỉ có DUY NHẤT 1 hàng chấm
        trajectoryLine.material.mainTextureScale = new Vector2(totalLength * dotDensity, 1f);

        // 3. ĐỘ TO: Đây mới là nút vặn để chấm to ra và hết bẹp!
        // Nếu dùng Shader Unlit/Transparent, tăng cái này là chấm to và tròn ngay.
        float lineSize = 0.5f;
        trajectoryLine.startWidth = lineSize;
        trajectoryLine.endWidth = lineSize;

        // Mẹo nhỏ: Nếu chấm vẫn hơi bẹp ngang, fen đừng chỉnh Y nữa, 
        // mà hãy giảm dotDensity xuống một chút (ví dụ từ 3.5 xuống 2.8).


    }

    private void Shoot(Vector2 direction)
    {
        canFire = false;
        trajectoryLine.positionCount = 0;

        // BẬT LẠI Collider để bóng có thể va chạm khi bay lên lưới
        Collider2D col = currentBubble.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        currentBubble.Fire(direction, fireSpeed);
        currentBubble = null;

        Invoke(nameof(ReloadBubble), 0.5f);
    }

    private void PrepareInitialBubbles()
    {
        // Tạo quả đầu tiên ở điểm chờ và tắt Collider ngay
        nextBubble = Instantiate(GetRandomPrefab(), nextBubblePoint.position, Quaternion.identity);
        ToggleBubbleCollider(nextBubble, false);

        ReloadBubble();
    }

    private void ReloadBubble()
    {
        currentBubble = nextBubble;

        // Tạo quả mới ở điểm chờ và tắt Collider
        nextBubble = Instantiate(GetRandomPrefab(), nextBubblePoint.position, Quaternion.identity);
        ToggleBubbleCollider(nextBubble, false);

        canFire = true;
    }

    private void ToggleBubbleCollider(Bubble bubble, bool state)
    {
        if (bubble != null)
        {
            Collider2D col = bubble.GetComponent<Collider2D>();
            if (col != null) col.enabled = state;
        }
    }

    private Bubble GetRandomPrefab()
    {
        return bubblePrefabs[Random.Range(0, bubblePrefabs.Length)];
    }
}