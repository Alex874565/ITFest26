using UnityEngine;
using System;
using UnityEngine.Serialization;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyMovementController))]
public class EnemyController : MonoBehaviour, IReachPlayer, IDisappear
{
    [SerializeField] private int preparationTime = 2;
    [SerializeField] private EquationsCategoriesDatabase equationsCategoriesDatabase;
    [SerializeField] private EnemyVisualController visualController;
    
    private EnemySpawner enemySpawner;
    
    public event Action<EquationType> OnDisappear;
    public event Action OnAttackFinished;

    private EnemyFactory _factory;
    
    public EquationType EquationType { get; private set; }
    private EquationData _equationData;
    private EnemyStats _stats;
    
    private EnemyMovementController _movementController;
    
    private delegate int GetCurrentLevelDelegate(EquationType equationType);
    private GetCurrentLevelDelegate GetDifficultyLevel;
    
    private void Awake()
    {
        _movementController = GetComponent<EnemyMovementController>();
        visualController.OnAttackAnimationFinished += FinishAttack;
    }

    private void OnEnable()
    {
        if(_movementController != null)
            _movementController.OnFlip += visualController.Flip;
    }

    private void OnDisable()
    {
        if (_movementController != null)
            _movementController.OnFlip -= visualController.Flip;
    }
    
    public void Instantiate(EquationType equationType, EnemyStats stats, PlayerController playerController)
    {
        Debug.Log(equationType);
        EquationType = equationType;
        _movementController.Instantiate(stats, playerController.gameObject.transform.position);
        GetDifficultyLevel = playerController.GetDifficultyLevel;
    }
    
    public bool CanDisappear(int answer)
    {
        if (answer == _equationData.Answer)
        {
            return true;
        }
        return false;
    }

    public void Disappear()
    {
        Debug.Log(_equationData.Type);
        OnDisappear?.Invoke(_equationData.Type);
        _factory.ReturnToPool(this);
    }

    public void ReachPlayer()
    {
        _movementController.ReachPlayer(preparationTime);
        visualController.ReachPlayer(preparationTime);
    }

    public void FinishAttack()
    {
        OnAttackFinished?.Invoke();
    }

    public void SetFactory(EnemyFactory factory)
    {
        _factory = factory;
    }
    
    public void ResetValues()
    {
        _equationData = GetRandomEquationData(EquationType);
        _movementController.ResetValues();
        visualController.ResetValues(_equationData);
    }
    
    private EquationData GetRandomEquationData(EquationType equationType)
    {
        EquationCategoryData categoryData = equationsCategoriesDatabase.GetEquationCategoryData(equationType);
        if (categoryData == null) return null;

        int difficultyLevel = GetDifficultyLevel(equationType);
        int max = GetMaxForLevel(categoryData, difficultyLevel);

        int x;
        int y;
        int answer;
        string equation;

        switch (equationType)
        {
            case EquationType.Addition:
                x = UnityEngine.Random.Range(1, max + 1);
                y = UnityEngine.Random.Range(1, max + 1);
                answer = x + y;
                equation = $"{x} + {y}";
                break;

            case EquationType.Subtraction:
                x = UnityEngine.Random.Range(1, max + 1);
                y = UnityEngine.Random.Range(1, max + 1);

                if (y > x)
                    (x, y) = (y, x);

                answer = x - y;
                equation = $"{x} - {y}";
                break;

            case EquationType.Multiplication:
                x = UnityEngine.Random.Range(1, max + 1);
                y = UnityEngine.Random.Range(1, max + 1);
                answer = x * y;
                equation = $"{x} × {y}";
                break;

            case EquationType.Division:
            {
                List<(int x, int y, int answer)> goodOptions = new List<(int, int, int)>();
                List<(int x, int y, int answer)> boringOptions = new List<(int, int, int)>();

                for (int divisor = 1; divisor <= max; divisor++)
                {
                    for (int quotient = 1; quotient <= max; quotient++)
                    {
                        int dividend = divisor * quotient;
                        if (dividend > max) continue;

                        var option = (dividend, divisor, quotient);

                        if (divisor == 1 || dividend == divisor || quotient == 1)
                            boringOptions.Add(option);
                        else
                            goodOptions.Add(option);
                    }
                }

                bool useBoring = goodOptions.Count == 0 || UnityEngine.Random.value < 0.1f; // 10% chance
                var pool = useBoring ? boringOptions : goodOptions;

                var choice = pool[UnityEngine.Random.Range(0, pool.Count)];
                x = choice.x;
                y = choice.y;
                answer = choice.answer;
                equation = $"{x} ÷ {y}";
                break;
            }

            default:
                return null;
        }

        return new EquationData(equationType, equation, answer);
    }

    private int GetMaxForLevel(EquationCategoryData categoryData, int difficultyLevel)
    {
        if (categoryData.DifficultyLevels == null || categoryData.DifficultyLevels.Count == 0)
        {
            Debug.LogWarning($"No difficulty levels. Defaulting to max=0.");
            return 0;
        }

        DifficultyLevelStats bestMatch = categoryData.DifficultyLevels[Mathf.Min(difficultyLevel, categoryData.DifficultyLevels.Count - 1)];

        return bestMatch.MaxNumber;
    }
    
    public int CountEnemiesSolvedBy(int number)
    {
        if (enemySpawner == null) return 0;

        int count = 0;
        for (int i = 0; i < enemySpawner.ActiveEnemies.Count; i++)
        {
            if (enemySpawner.ActiveEnemies[i].CanDisappear(number))
                count++;
        }

        return count;
    }
}