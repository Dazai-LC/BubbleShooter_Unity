using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "BubbleShooter/LevelData")]
public class LevelData : ScriptableObject
{
    public int levelNumber; // Số thứ tự màn chơi

    [TextArea(10, 15)]
    [Tooltip("0,1,2: Màu bóng | -1: Ô trống. Ngăn cách bằng dấu phẩy và xuống dòng.")]
    public string mapString;

    public int targetScore; // Điểm cần đạt để thắng (nếu fen muốn)
}