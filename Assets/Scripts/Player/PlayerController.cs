using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerVisualController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private NumberRecognitionController numberRecognitionController;
    [SerializeField] private EnemySpawner enemySpawner;
    
    public event Action OnScoreChanged;
    
    private PlayerVisualController _visualController;
    
    private int _score = 0;
    private Dictionary<EquationType, int> _equationScores;

    private void Awake()
    {
        _visualController = GetComponent<PlayerVisualController>();
        _equationScores = new Dictionary<EquationType, int>();
        numberRecognitionController.OnNumberRecognized += AttackEnemies;
        SaveManager.Instance.SelectedEquations.ForEach(equationType => _equationScores[equationType] = 0);
    }
    
    public void AddScore(EquationType equationType)
    {
        _equationScores[equationType]++;
        _score++;
        _visualController.UpdateScore(_score);
        OnScoreChanged?.Invoke();
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
}