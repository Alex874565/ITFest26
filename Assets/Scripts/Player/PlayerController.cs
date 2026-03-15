using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private NumberRecognitionController numberRecognitionController;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EquationsCategoriesDatabase equationsCategoriesDatabase;
    [SerializeField] private PlayerVisualController visualController;
    
    public event Action<int> OnScoreChanged;
    public event Action<Dictionary<EquationType, int>, int> OnDeathWithData;
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
        Money += SaveManager.Instance.EquationLevels[equationType] + 1;
        OnScoreChanged?.Invoke(Money);
    }

    public void AttackEnemies(int number)
    {
        float elapsed = _timeSinceEnemyDefeated;
        bool defeatedAny = false;

        for (int i = enemySpawner.ActiveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = enemySpawner.ActiveEnemies[i];

            if (enemy.CanDisappear(number))
            {
                ChangeDifficultyData(enemy.EquationType, elapsed);
                enemy.Disappear();
                defeatedAny = true;
            }
        }

        if (defeatedAny)
        {
            visualController.PlayHappyAnimation();
            _timeSinceEnemyDefeated = 0f;
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
    
    private const int RecentAnswersCount = 3;

    private void ChangeDifficultyData(EquationType equationType, float answerTime)
    {
        if (!_difficultyLevelsGameData.TryGetValue(equationType, out var gameData))
            return;

        var difficultyLevels = equationsCategoriesDatabase
            .GetEquationCategoryData(equationType)
            .DifficultyLevels;

        if (gameData.Level < 0 || gameData.Level >= difficultyLevels.Count)
            return;

        gameData.CorrectAnswers++;

        gameData.RecentAnswerTimes.Enqueue(answerTime);

        while (gameData.RecentAnswerTimes.Count > RecentAnswersCount)
            gameData.RecentAnswerTimes.Dequeue();

        float total = 0f;
        foreach (float time in gameData.RecentAnswerTimes)
            total += time;

        gameData.AnswerTimeAverage = total / gameData.RecentAnswerTimes.Count;

        DifficultyLevelStats difficultyLevelStats = difficultyLevels[gameData.Level];
        
        Debug.Log($"Equation: {equationType}, Level: {gameData.Level}, Correct Answers: {gameData.CorrectAnswers}, Answer Time Average: {gameData.AnswerTimeAverage:F2}s, Required Correct Answers: {difficultyLevelStats.MinAnswers}, Required Time: {difficultyLevelStats.PassThreshold:F2}s");

        if (gameData.CorrectAnswers >= difficultyLevelStats.MinAnswers &&
            gameData.AnswerTimeAverage <= difficultyLevelStats.PassThreshold)
        {
            if (gameData.Level < difficultyLevels.Count - 1)
                gameData.Level++;

            gameData.CorrectAnswers = 0;
            gameData.AnswerTimeAverage = 0f;
            gameData.RecentAnswerTimes.Clear();
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
        OnDeathWithData?.Invoke(_equationScores, Money);
        OnDeath?.Invoke();
    }
}