using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public enum GenerationMode { SafePattern, CustomLevelMap }

    [Header("Generation Settings")]
    [SerializeField] private GenerationMode mode = GenerationMode.SafePattern;

    [Header("Grid Size (Chỉ dùng cho Safe Pattern)")]
    [SerializeField] private int rows = 5;
    [SerializeField] private int columns = 18;

    [Header("Grid Transform")]
    [SerializeField] private float bubbleDiameter = 0.9f;
    [SerializeField] private Transform startPoint;

    [Header("Auto Drop Settings")]
    [SerializeField] private float dropInterval = 5f;
    [SerializeField] private float dropDistance = 0.5f;
    private float dropTimer = 0f;

    [Header("Prefabs (0: Green, 1: Red, 2: Yellow)")]
    [SerializeField] private Bubble[] bubblePrefabs;

    [Header("Level Design (Dùng cho Custom Level Map)")]
    [Tooltip("0,1,2: Màu bóng | -1: Ô trống")]
    [TextArea(10, 15)]
    [SerializeField]
    private string customLevelData =
        "-1,-1,-1,0,0,0,0,-1,-1,-1\n"+
        "-1,-1,1,1,1,1,1,-1,-1\n"+
        "-1,2,2,2,2,2,2,-1";

    [Header("Level System")]
    [SerializeField] private LevelDatabase levelDB; // Nhớ kéo file MyLevelDB vào đây trong Inspector

    private void Start()
    {
        // 1. Dọn dẹp Grid trước khi sinh
        foreach (Transform child in transform)
        {
            if (child != startPoint) Destroy(child.gameObject);
        }

        // 2. Lấy số màn chơi từ Menu (Mặc định là màn 1 nếu chưa chọn)
        int selectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);

        // 3. Chuyển từ số Level (1,2,3) sang Index mảng (0,1,2)
        int dbIndex = selectedLevel - 1;

        // 4. Kiểm tra và nạp dữ liệu từ Database
        if (levelDB != null && dbIndex >= 0 && dbIndex < levelDB.allLevels.Count)
        {
            LevelData data = levelDB.allLevels[dbIndex];
            customLevelData = data.mapString; // Lấy chuỗi thiết kế độc lạ
            mode = GenerationMode.CustomLevelMap; // Ép về chế độ vẽ tay

            Debug.Log($"<color=green>Đang nạp Màn {selectedLevel}</color> - Tên file: {data.name}");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy dữ liệu Level trong DB, chuyển về Safe Pattern mặc định.");
            mode = GenerationMode.SafePattern;
        }

        // 5. Chạy hàm sinh map
        if (mode == GenerationMode.SafePattern)
            GenerateSafePatternGrid();
        else
            GenerateGridFromCustomData();

        // --- GỌI HUD: ĐẾM SỐ BÓNG LÚC MỚI VÀO GAME ---
        // Phải đợi lưới sinh xong (0.1s) rồi mới gọi BubbleManager đếm và báo cáo HUD
        if (BubbleManager.Instance != null)
        {
            BubbleManager.Instance.Invoke("CheckWinCondition", 0.1f);
        }
    }

    // Đưa hàm này ra ngoài hẳn hàm Start
    [ContextMenu("Update Map From Code")]
    public void UpdateMap()
    {
        customLevelData = "-1,-1,-1,0,0,0,0,-1,-1,-1\n" +
                          "-1,-1,1,1,1,1,1,-1,-1\n" +
                          "-1,2,2,2,2,2,2,-1";

        Debug.Log("Đã cập nhật map mới vào Inspector! Nhớ nhấn Play để xem.");
    }

    private void Update()
    {
        if (Time.timeScale > 0)
        {
            dropTimer += Time.deltaTime;
            if (dropTimer >= dropInterval)
            {
                dropTimer = 0f;
                DropGrid();
            }
        }
    }

    private void DropGrid()
    {
        // Di chuyển cả GridGenerator xuống (vì bóng là con của nó nên sẽ đi theo)
        transform.Translate(Vector3.down * dropDistance, Space.World);
    }

    // ==========================================
    // CÁCH 1: TỰ ĐỘNG (Dùng biến rows/columns)
    // ==========================================
    private void GenerateSafePatternGrid()
    {
        float rowHeight = bubbleDiameter * Mathf.Sqrt(3) / 2f;

        for (int r = 0; r < rows; r++)
        {
            bool isOffsetRow = r % 2 != 0;
            int colsInRow = isOffsetRow ? columns - 1 : columns;

            for (int c = 0; c < colsInRow; c++)
            {
                float xPos = startPoint.position.x + (c * bubbleDiameter) + (isOffsetRow ? bubbleDiameter / 2f : 0f);
                float yPos = startPoint.position.y - (r * rowHeight);

                int colorID = ((c - r) % 3 + 3) % 3;
                SpawnBubbleAt(colorID, new Vector2(xPos, yPos));
            }
        }
    }

    // ==========================================
    // CÁCH 2: VẼ TAY (Tự động co dãn theo string)
    // ==========================================
    private void GenerateGridFromCustomData()
    {
        if (string.IsNullOrEmpty(customLevelData)) return;

        float rowHeight = bubbleDiameter * Mathf.Sqrt(3) / 2f;
        string[] rowsArr = customLevelData.Trim().Split('\n');

        for (int r = 0; r < rowsArr.Length; r++)
        {
            string[] colsArr = rowsArr[r].Split(',');
            bool isOffsetRow = r % 2 != 0;

            for (int c = 0; c < colsArr.Length; c++)
            {
                string val = colsArr[c].Trim();
                if (int.TryParse(val, out int bubbleID))
                {
                    // Nếu là -1 thì bỏ qua
                    if (bubbleID == -1) continue;

                    // Kiểm tra ID có tồn tại trong mảng Prefab không
                    if (bubbleID >= 0 && bubbleID < bubblePrefabs.Length)
                    {
                        float xPos = startPoint.position.x + (c * bubbleDiameter) + (isOffsetRow ? bubbleDiameter / 2f : 0f);
                        float yPos = startPoint.position.y - (r * rowHeight);

                        SpawnBubbleAt(bubbleID, new Vector2(xPos, yPos));
                    }
                }
            }
        }
    }

    private void SpawnBubbleAt(int id, Vector2 position)
    {
        if (bubblePrefabs[id] == null) return;

        Bubble newBubble = Instantiate(bubblePrefabs[id], position, Quaternion.identity, transform);
        newBubble.IsSnapped = true;
        // Đảm bảo Layer đúng để không bị xuyên thấu
        newBubble.gameObject.layer = LayerMask.NameToLayer("Bubble");
    }
}