using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "UnlockablesDatabase", menuName = "ScriptableObjects/Unlockables/Database")]
public class UnlockablesDatabase : ScriptableObject
{
    [field: SerializeField] public List<CategoryUnlockables> UnlockableCategories { get; private set; }

    private void OnValidate()
    {
        foreach (CategoryUnlockables category in UnlockableCategories)
        {
            foreach (UnlockableData unlockable in category.Unlockables)
            {
                unlockable.Type = category.Type;
            }
        }
    }

    public UnlockableData GetUnlockable(UnlockableType type, int index)
    {
        List<UnlockableData> unlockableDatas = UnlockableCategories.First(c => c.Type == type).Unlockables;
        if(index < 0 ||index >= unlockableDatas.Count)
        {
            Debug.LogError($"Index {index} is out of range for unlockables of type {type}");
            return null;
        }
        return unlockableDatas[index];
    }
}