using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemiesDatabase", menuName = "ScriptableObjects/EnemiesDatabase", order = 1)]
public class EnemiesDatabase : ScriptableObject
{
    [field: SerializeField] public List<EnemyData> Enemies { get; private set; }

    public EnemyData GetEnemyData(EquationType equationType)
    {
        return Enemies.Find(enemy => enemy.Stats.Type == equationType);
    }
}