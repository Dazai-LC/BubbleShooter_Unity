using System.Collections.Generic;
using UnityEngine;

public class BubbleManager : MonoBehaviour
{
    public static BubbleManager Instance { get; private set; }

    [Header("Grid & Match Settings")]
    [Tooltip("Đường kính bóng (PPU) - Phải bằng với số trong GridGenerator")]
    [SerializeField] private float bubbleDiameter = 1f;
    [Tooltip("Kéo object StartPoint vào đây để lấy mốc trần nhà")]
    [SerializeField] private Transform startPoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ProcessSnappedBubble(Bubble bubble)
    {
        SnapBubblePerfectly(bubble);
        CheckMatches(bubble);
    }

    // ==========================================
    // 1. CĂN CHỈNH BÓNG (Giữ nguyên - Hoạt động tốt)
    // ==========================================
    private void SnapBubblePerfectly(Bubble newBubble)
    {
        Collider2D[] touches = Physics2D.OverlapCircleAll(newBubble.transform.position, bubbleDiameter, LayerMask.GetMask("Bubble", "TopWall"));

        bool hitCeiling = false;
        Bubble closestNeighbor = null;
        float minDist = float.MaxValue;

        foreach (Collider2D hit in touches)
        {
            if (hit.gameObject == newBubble.gameObject) continue;

            if (hit.CompareTag("TopWall")) hitCeiling = true;
            else
            {
                Bubble neighbor = hit.GetComponent<Bubble>();
                if (neighbor != null && neighbor.IsSnapped)
                {
                    float d = Vector2.Distance(newBubble.transform.position, neighbor.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closestNeighbor = neighbor;
                    }
                }
            }
        }

        if (hitCeiling && startPoint != null)
        {
            float xDist = newBubble.transform.position.x - startPoint.position.x;
            int col = Mathf.RoundToInt(xDist / bubbleDiameter);
            float snapX = startPoint.position.x + (col * bubbleDiameter);
            newBubble.transform.position = new Vector2(snapX, startPoint.position.y);
        }
        else if (closestNeighbor != null)
        {
            Vector2 center = closestNeighbor.transform.position;
            float w = bubbleDiameter;
            float h = bubbleDiameter * Mathf.Sqrt(3) / 2f;
            float wHalf = w / 2f;

            Vector2[] hexOffsets = new Vector2[]
            {
                new Vector2(w, 0), new Vector2(-w, 0),
                new Vector2(wHalf, h), new Vector2(-wHalf, h),
                new Vector2(wHalf, -h), new Vector2(-wHalf, -h)
            };

            Vector2 bestPos = newBubble.transform.position;
            float minPosDist = float.MaxValue;

            foreach (Vector2 offset in hexOffsets)
            {
                Vector2 testPos = center + offset;
                Collider2D col = Physics2D.OverlapCircle(testPos, bubbleDiameter * 0.2f, LayerMask.GetMask("Bubble"));
                if (col != null && col.gameObject != newBubble.gameObject) continue;

                float d = Vector2.Distance(newBubble.transform.position, testPos);
                if (d < minPosDist)
                {
                    minPosDist = d;
                    bestPos = testPos;
                }
            }
            newBubble.transform.position = bestPos;
        }
    }

    // ==========================================
    // 2. TÌM BÓNG CÙNG MÀU ĐỂ NỔ
    // ==========================================
    private void CheckMatches(Bubble originBubble)
    {
        List<Bubble> allSnapped = GetAllFilesInScene();
        List<Bubble> matchedBubbles = new List<Bubble>();

        FindMatchesRecursive(originBubble, originBubble.Color, matchedBubbles, allSnapped);

        if (matchedBubbles.Count >= 3)
        {
            foreach (Bubble b in matchedBubbles)
            {
                b.Pop();
            }

            // Truyền danh sách bóng vừa nổ vào để BFS bỏ qua chúng
            DropFloatingBubbles(matchedBubbles, allSnapped);
        }
    }

    private void FindMatchesRecursive(Bubble current, BubbleColor targetColor, List<Bubble> matched, List<Bubble> allSnapped)
    {
        if (current == null || current.Color != targetColor || matched.Contains(current)) return;

        matched.Add(current);

        List<Bubble> neighbors = GetMathematicalNeighbors(current, allSnapped);
        foreach (Bubble neighbor in neighbors)
        {
            FindMatchesRecursive(neighbor, targetColor, matched, allSnapped);
        }
    }

    // ==========================================
    // 3. TÌM & RỤNG BÓNG MỒ CÔI
    // ==========================================
    private void DropFloatingBubbles(List<Bubble> destroyedBubbles, List<Bubble> allSnapped)
    {
        Queue<Bubble> roots = new Queue<Bubble>();
        HashSet<Bubble> connectedToCeiling = new HashSet<Bubble>();

        foreach (Bubble b in allSnapped)
        {
            if (destroyedBubbles.Contains(b)) continue;

            bool isAtCeiling = false;

            Collider2D[] touches = Physics2D.OverlapCircleAll(b.transform.position, bubbleDiameter * 0.8f);
            foreach (Collider2D hit in touches)
            {
                if (hit.CompareTag("TopWall"))
                {
                    isAtCeiling = true;
                    break;
                }
            }

            if (startPoint != null && Mathf.Abs(b.transform.position.y - startPoint.position.y) < (bubbleDiameter * 0.5f))
            {
                isAtCeiling = true;
            }

            if (isAtCeiling)
            {
                roots.Enqueue(b);
                connectedToCeiling.Add(b);
            }
        }

        while (roots.Count > 0)
        {
            Bubble current = roots.Dequeue();
            List<Bubble> neighbors = GetMathematicalNeighbors(current, allSnapped);

            foreach (Bubble neighbor in neighbors)
            {
                if (!destroyedBubbles.Contains(neighbor) && !connectedToCeiling.Contains(neighbor))
                {
                    connectedToCeiling.Add(neighbor);
                    roots.Enqueue(neighbor);
                }
            }
        }

        foreach (Bubble b in allSnapped)
        {
            if (!destroyedBubbles.Contains(b) && !connectedToCeiling.Contains(b))
            {
                b.Drop();
            }
        }
    }

    // ==========================================
    // HÀM HỖ TRỢ
    // ==========================================
    private List<Bubble> GetMathematicalNeighbors(Bubble target, List<Bubble> allSnapped)
    {
        List<Bubble> neighbors = new List<Bubble>();
        float maxDistance = bubbleDiameter * 1.35f;

        foreach (Bubble b in allSnapped)
        {
            if (b != target)
            {
                if (Vector2.Distance(target.transform.position, b.transform.position) <= maxDistance)
                {
                    neighbors.Add(b);
                }
            }
        }
        return neighbors;
    }

    private List<Bubble> GetAllFilesInScene()
    {
        // [CẬP NHẬT UNITY 6000] Thay thế FindObjectsOfType bằng FindObjectsByType
        Bubble[] arr = FindObjectsByType<Bubble>(FindObjectsSortMode.None);
        List<Bubble> list = new List<Bubble>();
        foreach (Bubble b in arr)
        {
            if (b.IsSnapped) list.Add(b);
        }
        return list;
    }
}