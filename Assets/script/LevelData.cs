using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int levelNumber;
    public int targetScore;
    public List<Vector2Int> preFilledBlocks = new List<Vector2Int>();
    public List<List<Vector2Int>> obstacleGroups = new List<List<Vector2Int>>();
    
    public LevelData(int number, int target)
    {
        levelNumber = number;
        targetScore = target;
    }
}
