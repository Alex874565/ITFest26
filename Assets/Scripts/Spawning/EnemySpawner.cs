using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyFactory _enemyFactory;

    [Header("Spawn Timing")]
    [SerializeField] private AnimationCurve _spawnTimeCurve = AnimationCurve.Linear(0f, 2f, 60f, 0.25f);
    [SerializeField] private float _curveDuration = 60f;
    [SerializeField] private float _minSpawnTime = 0.25f;
    [SerializeField] private float _maxSpawnTime = 5f;

    private List<EquationType> _allowedTypes;
    private BoxCollider2D _boxCollider2D;

    private float _spawnTimer;
    private float _elapsedTime;

    private void Awake()
    {
        _boxCollider2D = GetComponent<BoxCollider2D>();
        //_allowedTypes = SaveManager.Instance.SelectedEquations;
        _allowedTypes = new List<EquationType>()
        {
            EquationType.Addition,
            EquationType.Subtraction,
            EquationType.Multiplication,
            EquationType.Division,
        };
    }

    private void Start()
    {
        _spawnTimer = GetCurrentSpawnTime();
    }

    private void Update()
    {
        if (_allowedTypes == null || _allowedTypes.Count == 0)
            return;

        _elapsedTime += Time.deltaTime;
        _spawnTimer -= Time.deltaTime;

        if (_spawnTimer <= 0f)
        {
            SpawnEnemy();
            _spawnTimer = GetCurrentSpawnTime();
        }
    }

    private float GetCurrentSpawnTime()
    {
        float timeOnCurve = Mathf.Min(_elapsedTime, _curveDuration);
        float spawnTime = _spawnTimeCurve.Evaluate(timeOnCurve);
        return Mathf.Clamp(spawnTime, _minSpawnTime, _maxSpawnTime);
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();
        EquationType enemyType = GetRandomEnemyType();
        _enemyFactory.SpawnEnemy(enemyType, spawnPosition);
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Bounds bounds = _boxCollider2D.bounds;

        float minX = bounds.min.x;
        float maxX = bounds.max.x;
        float minY = bounds.min.y;
        float maxY = bounds.max.y;

        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0:
                return new Vector2(Random.Range(minX, maxX), maxY);
            case 1:
                return new Vector2(Random.Range(minX, maxX), minY);
            case 2:
                return new Vector2(minX, Random.Range(minY, maxY));
            case 3:
                return new Vector2(maxX, Random.Range(minY, maxY));
            default:
                return Vector2.zero;
        }
    }

    private EquationType GetRandomEnemyType()
    {
        int randomIndex = Random.Range(0, _allowedTypes.Count);
        return _allowedTypes[randomIndex];
    }
}