using Microsoft.Unity.VisualStudio.Editor;
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

    [Header("UI References")]
    public GameObject winPanel; // Kéo WinPanel vào đây
    public GridGenerator gridGen;

    // Khai báo biến này ở đầu file, cùng khu vực với các biến khác
    private int currentChainCombo = 0;

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
        if (gridGen != null && bubble.transform.parent != gridGen.transform)
        {
            bubble.transform.SetParent(gridGen.transform);
        }

        SnapBubblePerfectly(bubble);
        CheckMatches(bubble);
    }

    // ==========================================
    // 1. CĂN CHỈNH BÓNG (Sử dụng Local Position)
    // ==========================================
    private void SnapBubblePerfectly(Bubble newBubble)
    {
        Collider2D[] touches = Physics2D.OverlapCircleAll(newBubble.transform.position, bubbleDiameter, LayerMask.GetMask("Bubble"));

        Bubble closestNeighbor = null;
        float minDist = float.MaxValue;

        foreach (Collider2D hit in touches)
        {
            if (hit.gameObject == newBubble.gameObject) continue;

            Bubble neighbor = hit.GetComponent<Bubble>();
            if (neighbor != null && neighbor.IsSnapped && neighbor.gameObject.layer != LayerMask.NameToLayer("FallingBubble"))
            {
                float d = Vector2.Distance(newBubble.transform.localPosition, neighbor.transform.localPosition);
                if (d < minDist)
                {
                    minDist = d;
                    closestNeighbor = neighbor;
                }
            }
        }

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
            // 1. TĂNG CHUỖI COMBO (Bắn trúng liên tiếp)
            currentChainCombo++;

            // 2. TÍNH ĐIỂM
            int basePoints = matchedBubbles.Count * 10; // Điểm cơ bản
            int simultaneousBonus = (matchedBubbles.Count - 3) * 15; // Nổ càng to thưởng càng gắt
            int pointsEarned = (basePoints + simultaneousBonus) * currentChainCombo; // Nhân với hệ số liên hoàn

            // 3. BÁO CÁO LÊN UI
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.AddScore(pointsEarned);

                // Chỉ hiện chữ Combo giữa màn hình khi nổ từ 4 quả trở lên HOẶC đang có chuỗi x2 trở lên
                if (matchedBubbles.Count > 3 || currentChainCombo > 1)
                {
                    HUDManager.Instance.ShowComboAction(currentChainCombo, matchedBubbles.Count, pointsEarned);
                }
            }

            foreach (Bubble b in matchedBubbles)
            {
                // Truyền chuỗi combo vào Pop để sau này fen có thể làm âm thanh nổ to nhỏ theo Combo
                b.Pop(currentChainCombo);
            }

            DropFloatingBubbles(matchedBubbles, allSnapped);
            Invoke("CheckWinCondition", 0.3f);
        }
        else
        {
            // BẮN TRƯỢT -> ĐỨT CHUỖI COMBO, RESET VỀ 0
            currentChainCombo = 0;
            Invoke("CheckWinCondition", 0.1f);
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

            if (startPoint != null && Mathf.Abs(b.transform.localPosition.y - startPoint.localPosition.y) < (bubbleDiameter * 0.5f))
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

        // GỌI KIỂM TRA THẮNG TẠI ĐÂY
        Invoke("CheckWinCondition", 0.3f);
    }

    // ==========================================
    // HÀM HỖ TRỢ
    // ==========================================
    private List<Bubble> GetMathematicalNeighbors(Bubble target, List<Bubble> allSnapped)
    {
        List<Bubble> neighbors = new List<Bubble>();
        float maxDistance = bubbleDiameter * 1.2f;

        foreach (Bubble b in allSnapped)
        {
            if (b != target && b != null)
            {
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

    // ==========================================
    // 4. KIỂM TRA ĐIỀU KIỆN THẮNG CUỘC
    // ==========================================
    public void CheckWinCondition()
    {
        int bubbleCount = 0;
        if (gridGen == null) return;

        int fallingLayer = LayerMask.NameToLayer("FallingBubble");

        foreach (Transform child in gridGen.transform)
        {
            Bubble b = child.GetComponent<Bubble>();

            // ĐIỀU KIỆN CỐT LÕI: Có script Bubble + Đang Active + KHÔNG PHẢI bóng đang rơi
            if (b != null && child.gameObject.activeSelf && child.gameObject.layer != fallingLayer)
            {
                bubbleCount++;
            }
        }

        // --- GỌI HUD: CẬP NHẬT MỤC TIÊU CÒN LẠI ---
        if (HUDManager.Instance != null) HUDManager.Instance.UpdateGridBalls(bubbleCount);

        if (bubbleCount <= 0)
        {
            Debug.Log("<color=green>!!! CHIẾN THẮNG !!! Đã dọn sạch bàn.</color>");
            Invoke("ShowWinUI", 0.5f);
        }
        else
        {
            Debug.Log($"<color=orange>Lưới còn {bubbleCount} quả bóng.</color>");
        }
    }

    private void ShowWinUI()
    {
        if (winPanel != null) winPanel.SetActive(true);
    }
}