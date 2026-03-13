using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EquationsCategoriesDatabase", menuName = "ScriptableObjects/EquationsCategoriesDatabase")]
public class EquationsCategoriesDatabase : ScriptableObject
{
    [SerializeField] private List<EquationCategoryData> equationsDatabase;

    public EquationCategoryData GetEquationCategoryData(EquationType equationType)
    {
        return equationsDatabase.Find(category => category.Type == equationType);
    }
}