using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "BubbleShooter/LevelDatabase")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelData> allLevels; // Kéo tất cả các file LevelData vào đây
}