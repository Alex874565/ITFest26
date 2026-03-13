using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquationCategoryData", menuName = "ScriptableObjects/EquationCategoryData")]
public class EquationCategoryData : ScriptableObject
{
    [field: SerializeField] public EquationType Type { get; private set; }
    [field: SerializeField] public List<int> AchievmentThresholds { get; private set; }
    [field: SerializeField] public List<EquationData> Equations { get; private set; }

    private void OnEnable()
    {
        foreach (var equation in Equations)
        {
            equation.Type = Type;
        }
    }
    
    private void OnValidate()
    {
        foreach (var equation in Equations)
        {
            equation.Type = Type;
        }
    }
}