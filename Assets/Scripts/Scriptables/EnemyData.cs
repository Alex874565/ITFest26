using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "ScriptableObjects/EnemyData", order = 1)]
public class EnemyData : ScriptableObject
{
    [field: SerializeField] public EnemyStats Stats { get; private set; }
    [field: SerializeField] public GameObject Prefab { get; private set; }
    
    public EnemyData(EnemyData enemyData)
    {
        Stats = enemyData.Stats;
        Prefab = enemyData.Prefab;
    }
}