using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyFactory : MonoBehaviour
{
    [SerializeField] private EnemiesDatabase enemiesDatabase;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxPoolSize = 50;

    public List<EnemyController> ActiveEnemies { get; private set; } = new List<EnemyController>();
    
    private readonly Dictionary<EquationType, ObjectPool<EnemyController>> _pools = new();
    private readonly Dictionary<EnemyController, ObjectPool<EnemyController>> _enemyToPool = new();

    private void Awake()
    {
        foreach (EnemyData enemyData in enemiesDatabase.Enemies)
        {
            if (!_pools.ContainsKey(enemyData.Stats.Type))
                _pools.Add(enemyData.Stats.Type, CreatePool(enemyData, enemyData.Stats.Type));
        }
    }

    public EnemyController SpawnEnemy(EquationType equationType, Vector2 spawnPosition)
    {
        if (!_pools.TryGetValue(equationType, out ObjectPool<EnemyController> pool))
        {
            EnemyData enemyData = enemiesDatabase.GetEnemyData(equationType);
            if (enemyData == null)
                return null;

            pool = CreatePool(enemyData, equationType);
            _pools.Add(equationType, pool);
        }

        EnemyController enemy = pool.Get();
        enemy.transform.position = spawnPosition;
        enemy.transform.rotation = Quaternion.identity;
        return enemy;
    }

    public void ReturnToPool(EnemyController enemy)
    {
        if (enemy != null && _enemyToPool.TryGetValue(enemy, out ObjectPool<EnemyController> pool))
        {
            pool.Release(enemy);
        }
    }

    private ObjectPool<EnemyController> CreatePool(EnemyData enemyData, EquationType equationType)
    {
        ObjectPool<EnemyController> pool = null;

        pool = new ObjectPool<EnemyController>(
            createFunc: () =>
            {
                GameObject instance = Instantiate(enemyData.Prefab, transform);
                EnemyController enemy = instance.GetComponent<EnemyController>();

                enemy.Instantiate(equationType, enemyData.Stats, playerController);
                enemy.SetFactory(this);

                _enemyToPool[enemy] = pool;
                instance.SetActive(false);

                return enemy;
            },
            actionOnGet: enemy =>
            {
                enemy.gameObject.SetActive(true);
                enemy.ResetValues();
                enemy.OnDisappear += playerController.AddScore;
                enemy.OnAttackFinished += playerController.Die;
                
                ActiveEnemies.Add(enemy);
            },
            actionOnRelease: enemy =>
            {
                enemy.OnAttackFinished -= playerController.Die;
                enemy.OnDisappear -= playerController.AddScore;
                enemy.gameObject.SetActive(false);
                ActiveEnemies.Remove(enemy);
            },
            actionOnDestroy: enemy =>
            {
                if (enemy != null)
                {
                    enemy.OnAttackFinished -= playerController.Die;
                    enemy.OnDisappear -= playerController.AddScore;
                    ActiveEnemies.Remove(enemy);
                    Destroy(enemy.gameObject);
                }
            },
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxPoolSize
        );

        return pool;
    }
}