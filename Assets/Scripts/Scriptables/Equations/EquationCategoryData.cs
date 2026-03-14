using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquationCategoryData", menuName = "ScriptableObjects/EquationCategoryData")]
public class EquationCategoryData : ScriptableObject
{
    [field: SerializeField] public EquationType Type { get; private set; }
    [field: SerializeField] public List<int> AchievmentThresholds { get; private set; }
    [field: SerializeField] public List<DifficultyLevelStats> DifficultyLevels { get; private set; }

    public EquationCategoryData(EquationCategoryData equationCategoryData)
    {
        Type = equationCategoryData.Type;
        AchievmentThresholds = new List<int>(equationCategoryData.AchievmentThresholds);
    }
}