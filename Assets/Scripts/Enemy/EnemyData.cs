using System;
using UnityEngine;

[Serializable]
public class EnemyStats
{
    [field: SerializeField] public EquationType Type { get; private set; }
    [field: SerializeField] public float MovementSpeed { get; private set; }
    
    public EnemyStats(EnemyStats stats)
    {
        Type = stats.Type;
        MovementSpeed = stats.MovementSpeed;
    }
}