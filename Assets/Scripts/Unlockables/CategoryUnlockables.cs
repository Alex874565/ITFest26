using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CategoryUnlockables
{
    [field: SerializeField] public UnlockableType Type { get; private set; }
    [field: SerializeField] public List<UnlockableData> Unlockables { get; private set; }
}