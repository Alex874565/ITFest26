using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerVisualController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private NumberRecognitionController numberRecognitionController;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EquationsCategoriesDatabase equationsCategoriesDatabase;

    public event Action<int> OnScoreChanged;
    public event Action<Dictionary<EquationType, int>> OnDeathWithData;
    public event Action OnDeath;

    private Dictionary<EquationType, DifficultyLevelGameData> _difficultyLevelsGameData = new Dictionary<EquationType, DifficultyLevelGameData>();
    
    public int Money { get; private set; }
    private Dictionary<EquationType, int> _equationScores;

    private float _timeSinceEnemyDefeated;

    private void Awake()
    {
        _equationScores = new Dictionary<EquationType, int>();
        SaveManager.Instance.SelectedEquations.ForEach(equationType =>
        {
            _equationScores[equationType] = 0;
            _difficultyLevelsGameData[equationType] = new DifficultyLevelGameData(0, 0, 0);
        });
    }

    private void Start()
    {
        Money = 0;
        OnScoreChanged?.Invoke(Money);
    }

    private void OnEnable()
    {
        if(numberRecognitionController != null)
            numberRecognitionController.OnNumberRecognized += AttackEnemies;
    }

    private void OnDisable()
    {
        if (numberRecognitionController != null)
            numberRecognitionController.OnNumberRecognized -= AttackEnemies;
    }

    private void Update()
    {
        _timeSinceEnemyDefeated += Time.deltaTime;
    }
    
    public void AddScore(EquationType equationType)
    {
        _equationScores[equationType]++;
        Money++;
        OnScoreChanged?.Invoke(Money);
    }

    public void AttackEnemies(int number)
    {
        for (int i = enemySpawner.ActiveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = enemySpawner.ActiveEnemies[i];

            if (enemy.CanDisappear(number))
            {
                ChangeDifficultyData(enemy.EquationType);
                _timeSinceEnemyDefeated = 0f;
                enemy.Disappear();
            }
        }
    }
    public bool TryGetBestAssistDistance(int number, out float closestDistance)
    {
        closestDistance = float.PositiveInfinity;

        if (enemySpawner == null || enemySpawner.ActiveEnemies == null)
            return false;

        Vector3 playerPosition = transform.position;
        bool foundMatch = false;

        for (int i = 0; i < enemySpawner.ActiveEnemies.Count; i++)
        {
            EnemyController enemy = enemySpawner.ActiveEnemies[i];
            if (enemy == null || !enemy.CanDisappear(number))
                continue;

            float distance = Vector3.Distance(playerPosition, enemy.transform.position);

            if (distance < closestDistance)
                closestDistance = distance;

            foundMatch = true;
        }

        return foundMatch;
    }
    
    private void ChangeDifficultyData(EquationType equationType)
    {
        if (!_difficultyLevelsGameData.ContainsKey(equationType))
            return;

        DifficultyLevelGameData gameData = _difficultyLevelsGameData[equationType];
        gameData.CorrectAnswers++;
        gameData.AnswerTimeAverage = (gameData.AnswerTimeAverage + _timeSinceEnemyDefeated)/gameData.CorrectAnswers;

        DifficultyLevelStats difficultyLevelStats = equationsCategoriesDatabase.GetEquationCategoryData(equationType).DifficultyLevels[gameData.Level];
        if(gameData.CorrectAnswers >= difficultyLevelStats.MinAnswers && gameData.AnswerTimeAverage <= difficultyLevelStats.PassThreshold)
        {
            Debug.Log(gameData.Level + 1);
            Debug.Log(equationsCategoriesDatabase.GetEquationCategoryData(equationType).DifficultyLevels[gameData.Level + 1].MaxNumber);

            gameData.Level++;
            gameData.CorrectAnswers = 0;
            gameData.AnswerTimeAverage = 0;
        }
        
        _difficultyLevelsGameData[equationType] = gameData;
    }

    public int GetDifficultyLevel(EquationType equationType)
    {
        Debug.Log(equationType + " " + _difficultyLevelsGameData[equationType].Level);
        return _difficultyLevelsGameData[equationType].Level;
    }

    public void Die()
    {
        OnDeathWithData?.Invoke(_equationScores);
        OnDeath?.Invoke();
    }
}