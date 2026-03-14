using UnityEngine;

[System.Serializable]
public class UnlockableData
{
    [field: SerializeField] public UnlockableType  Type { get; set; }
    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public int Cost { get; private set; }
}