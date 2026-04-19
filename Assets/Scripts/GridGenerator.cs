using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public enum GenerationMode { SafePattern, CustomLevelMap }

    [Header("Generation Settings")]
    [Tooltip("Chọn cách tạo Map: Tự động xen kẽ an toàn HOẶC Vẽ map bằng tay")]
    [SerializeField] private GenerationMode mode = GenerationMode.SafePattern;

    [Header("Grid Size (Dành cho Safe Pattern)")]
    [SerializeField] private int rows = 5;
    [SerializeField] private int columns = 18;

    [Header("Grid Transform")]
    [SerializeField] private float bubbleDiameter = 0.9f;
    [SerializeField] private Transform startPoint;

    [Header("Auto Drop Settings")]
    [SerializeField] private float dropInterval = 5f;   // 5 giây tụt 1 lần
    [SerializeField] private float dropDistance = 0.5f; // Tụt xuống nửa mét
    private float dropTimer = 0f;

    [Header("Prefabs (0: Green, 1: Red, 2: Yellow)")]
    [SerializeField] private Bubble[] bubblePrefabs;

    [Header("Level Design (Dành cho Custom Level Map)")]
    [Tooltip("0: Green | 1: Red | 2: Yellow | -1: Vị trí Trống")]
    [TextArea(5, 10)] // Tạo khung nhập liệu siêu to trong Inspector
    [SerializeField]
    private string customLevelData =
        "0,1,2,0,1,2,0,1,2,0,1,2,0,1,2,0,1,2\n" +
        "1,2,0,1,2,0,1,2,0,1,2,0,1,2,0,1,2\n" +
        "-1,-1,0,1,2,0,1,2,0,1,2,-1,-1,-1,-1,-1,-1\n" +
        "-1,-1,-1,1,2,0,1,2,0,1,-1,-1,-1,-1,-1,-1,-1";

    private void Start()
    {
        if (mode == GenerationMode.SafePattern)
        {
            GenerateSafePatternGrid();
        }
        else
        {
            GenerateGridFromCustomData();
        }
    }

    // Trong GridGenerator.cs
    private void Update()
    {
        if (Time.timeScale > 0)
        {
            dropTimer += Time.deltaTime;

            if (dropTimer >= dropInterval)
            {
                // Reset timer TRƯỚC khi chạy hàm để tránh bị gọi chồng chéo
                dropTimer = 0f;
                DropGrid();
            }
        }
    }

    private void DropGrid()
    {
        // Thêm dòng này để kiểm tra xem có bao nhiêu thằng đang gọi hàm này
        Debug.Log($"<color=red>[ID: {gameObject.GetInstanceID()}]</color> Lưới tụt lúc: " + Time.time);

        transform.Translate(Vector3.down * dropDistance, Space.World);
    }

    // ==========================================
    // CÁCH 1: THUẬT TOÁN TOÁN HỌC KHÔNG BAO GIỜ TRÙNG
    // ==========================================
    private void GenerateSafePatternGrid()
    {
        float rowHeight = bubbleDiameter * Mathf.Sqrt(3) / 2f;

        for (int row = 0; row < rows; row++)
        {
            bool isOffsetRow = row % 2 != 0;
            int colsInRow = isOffsetRow ? columns - 1 : columns;

            for (int col = 0; col < colsInRow; col++)
            {
                float xPos = startPoint.position.x + (col * bubbleDiameter) + (isOffsetRow ? bubbleDiameter / 2f : 0f);
                float yPos = startPoint.position.y - (row * rowHeight);

                Vector2 spawnPos = new Vector2(xPos, yPos);

                // Công thức Hexagon 3-Color Theorem: Đảm bảo 100% không có 2 bóng cùng màu nằm cạnh nhau
                int colorID = ((col - row) % 3 + 3) % 3;

                SpawnBubbleAt(colorID, spawnPos);
            }
        }
    }

    // ==========================================
    // CÁCH 2: TỰ VẼ MAP TỪ MẢNG (LEVEL DESIGN)
    // ==========================================
    private void GenerateGridFromCustomData()
    {
        float rowHeight = bubbleDiameter * Mathf.Sqrt(3) / 2f;

        // Tách các hàng dựa vào dấu xuống dòng (Enter)
        string[] rowsData = customLevelData.Trim().Split('\n');

        for (int row = 0; row < rowsData.Length; row++)
        {
            // Tách các ID trong 1 hàng dựa vào dấu phẩy
            string[] colsData = rowsData[row].Split(',');
            bool isOffsetRow = row % 2 != 0;

            for (int col = 0; col < colsData.Length; col++)
            {
                string idString = colsData[col].Trim();
                if (string.IsNullOrEmpty(idString)) continue;

                if (int.TryParse(idString, out int bubbleID))
                {
                    // Nếu nhập -1 (Lỗ hổng) thì bỏ qua, không spawn bóng
                    if (bubbleID < 0 || bubbleID >= bubblePrefabs.Length) continue;

                    float xPos = startPoint.position.x + (col * bubbleDiameter) + (isOffsetRow ? bubbleDiameter / 2f : 0f);
                    float yPos = startPoint.position.y - (row * rowHeight);

                    SpawnBubbleAt(bubbleID, new Vector2(xPos, yPos));
                }
            }
        }
    }

    private void SpawnBubbleAt(int id, Vector2 position)
    {
        Bubble prefab = bubblePrefabs[id];
        // Tham số cuối cùng 'transform' chính là gán GridGenerator làm cha
        Bubble newBubble = Instantiate(prefab, position, Quaternion.identity, transform);
        newBubble.IsSnapped = true;
    }
}