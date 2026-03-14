using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerVisualController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private NumberRecognitionController numberRecognitionController;
    [SerializeField] private EnemySpawner enemySpawner;

    public event Action<int> OnScoreChanged;
    public event Action<Dictionary<EquationType, int>> OnDeathWithData;
    public event Action OnDeath;
    
    private PlayerVisualController _visualController;
    
    private int _score = 0;
    private Dictionary<EquationType, int> _equationScores;

    private void Awake()
    {
        _visualController = GetComponent<PlayerVisualController>();
        _equationScores = new Dictionary<EquationType, int>();
        SaveManager.Instance.SelectedEquations.ForEach(equationType => _equationScores[equationType] = 0);
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
    
    public void AddScore(EquationType equationType)
    {
        _equationScores[equationType]++;
        _score++;
        _visualController.UpdateScore(_score);
        OnScoreChanged?.Invoke(_score);
    }

    public void AttackEnemies(int number)
    {
        for (int i = enemySpawner.ActiveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = enemySpawner.ActiveEnemies[i];

            if (enemy.CanDisappear(number))
            {
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

    public void Die()
    {
        OnDeathWithData?.Invoke(_equationScores);
        OnDeath?.Invoke();
    }
}