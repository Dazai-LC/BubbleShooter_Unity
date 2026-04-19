using System.Collections.Generic;
using UnityEngine;

public class BubbleManager : MonoBehaviour
{
    public static BubbleManager Instance { get; private set; }

    [Header("Grid & Match Settings")]
    [Tooltip("Đường kính bóng (PPU) - Phải bằng với số trong GridGenerator")]
    [SerializeField] private float bubbleDiameter = 0.9f;

    [Tooltip("Kéo object StartPoint vào đây để lấy mốc trần nhà")]
    [SerializeField] private Transform startPoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // 0. ĐIỀU PHỐI CHÍNH
    // ==========================================
    public void ProcessSnappedBubble(Bubble bubble)
    {
        // Đảm bảo bóng đã thuộc về Grid để localPosition hoạt động chính xác
        GridGenerator grid = FindFirstObjectByType<GridGenerator>();
        if (grid != null && bubble.transform.parent != grid.transform)
        {
            bubble.transform.SetParent(grid.transform);
        }

        SnapBubblePerfectly(bubble);
        CheckMatches(bubble);
    }

    // ==========================================
    // 1. CĂN CHỈNH BÓNG (Sử dụng Local Position)
    // ==========================================
    private void SnapBubblePerfectly(Bubble newBubble)
    {
        // Tìm hàng xóm gần nhất bằng Physics (World) để lấy mốc
        Collider2D[] touches = Physics2D.OverlapCircleAll(newBubble.transform.position, bubbleDiameter, LayerMask.GetMask("Bubble"));

        Bubble closestNeighbor = null;
        float minDist = float.MaxValue;

        foreach (Collider2D hit in touches)
        {
            if (hit.gameObject == newBubble.gameObject) continue;

            Bubble neighbor = hit.GetComponent<Bubble>();
            if (neighbor != null && neighbor.IsSnapped && neighbor.gameObject.layer != LayerMask.NameToLayer("FallingBubble"))
            {
                // Tính khoảng cách theo LOCAL để đồng bộ
                float d = Vector2.Distance(newBubble.transform.localPosition, neighbor.transform.localPosition);
                if (d < minDist)
                {
                    minDist = d;
                    closestNeighbor = neighbor;
                }
            }
        }

        // Logic Snap ưu tiên trần nhà
        if (startPoint != null && newBubble.transform.localPosition.y >= startPoint.localPosition.y - (bubbleDiameter * 0.4f))
        {
            float xDist = newBubble.transform.localPosition.x - startPoint.localPosition.x;
            int col = Mathf.RoundToInt(xDist / bubbleDiameter);
            float snapX = startPoint.localPosition.x + (col * bubbleDiameter);

            newBubble.transform.localPosition = new Vector2(snapX, startPoint.localPosition.y);
            return;
        }
        else if (closestNeighbor != null)
        {
            Vector2 center = closestNeighbor.transform.localPosition;
            float w = bubbleDiameter;
            float h = bubbleDiameter * Mathf.Sqrt(3) / 2f;
            float wHalf = w / 2f;

            Vector2[] hexOffsets = new Vector2[]
            {
                new Vector2(w, 0), new Vector2(-w, 0),
                new Vector2(wHalf, h), new Vector2(-wHalf, h),
                new Vector2(wHalf, -h), new Vector2(-wHalf, -h)
            };

            Vector2 bestLocalPos = newBubble.transform.localPosition;
            float minPosDist = float.MaxValue;

            foreach (Vector2 offset in hexOffsets)
            {
                Vector2 testLocalPos = center + offset;

                // Check đè (dùng World position để check vật lý chuẩn nhất)
                Vector2 testWorldPos = newBubble.transform.parent.TransformPoint(testLocalPos);
                Collider2D overlapping = Physics2D.OverlapCircle(testWorldPos, bubbleDiameter * 0.2f, LayerMask.GetMask("Bubble"));
                if (overlapping != null && overlapping.gameObject != newBubble.gameObject) continue;

                float d = Vector2.Distance(newBubble.transform.localPosition, testLocalPos);
                if (d < minPosDist)
                {
                    minPosDist = d;
                    bestLocalPos = testLocalPos;
                }
            }
            newBubble.transform.localPosition = bestLocalPos;
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
            int comboMultiplier = 1;
            if (matchedBubbles.Count >= 5) comboMultiplier = 2;
            if (matchedBubbles.Count >= 8) comboMultiplier = 3;

            foreach (Bubble b in matchedBubbles)
            {
                b.Pop(comboMultiplier);
            }

            // Sau khi nổ, kiểm tra rụng dựa trên danh sách bóng còn lại
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
    // 3. TÌM & RỤNG BÓNG MỒ CÔI (Bản Fix Local)
    // ==========================================
    private void DropFloatingBubbles(List<Bubble> destroyedBubbles, List<Bubble> allSnapped)
    {
        Queue<Bubble> roots = new Queue<Bubble>();
        HashSet<Bubble> connectedToCeiling = new HashSet<Bubble>();

        foreach (Bubble b in allSnapped)
        {
            if (destroyedBubbles.Contains(b)) continue;

            bool isAtCeiling = false;

            // Check dính trần dựa vào localPosition của startPoint
            if (startPoint != null)
            {
                // So sánh tọa độ Y cục bộ của bóng và trần nhà
                if (Mathf.Abs(b.transform.localPosition.y - startPoint.localPosition.y) < (bubbleDiameter * 0.5f))
                {
                    isAtCeiling = true;
                }
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
    // HÀM HỖ TRỢ (Dùng Local Position)
    // ==========================================
    private List<Bubble> GetMathematicalNeighbors(Bubble target, List<Bubble> allSnapped)
    {
        List<Bubble> neighbors = new List<Bubble>();
        // 1.2f là con số vàng cho lưới hexagon
        float maxDistance = bubbleDiameter * 1.2f;

        foreach (Bubble b in allSnapped)
        {
            if (b != target && b != null)
            {
                // QUAN TRỌNG: Tính khoảng cách theo Local để không bị ảnh hưởng khi lưới tụt
                float dist = Vector2.Distance(target.transform.localPosition, b.transform.localPosition);
                if (dist <= maxDistance)
                {
                    neighbors.Add(b);
                }
            }
        }
        return neighbors;
    }

    private List<Bubble> GetAllFilesInScene()
    {
        Bubble[] arr = FindObjectsByType<Bubble>(FindObjectsSortMode.None);
        List<Bubble> list = new List<Bubble>();
        int fallingLayer = LayerMask.NameToLayer("FallingBubble");

        foreach (Bubble b in arr)
        {
            if (b.IsSnapped && b.gameObject.layer != fallingLayer)
            {
                list.Add(b);
            }
        }
        return list;
    }
}