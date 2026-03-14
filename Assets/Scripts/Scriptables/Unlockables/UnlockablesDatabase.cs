using UnityEngine;
using System.Collections.Generic;

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
}