using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyMovementController), typeof(EnemyVisualController))]
public class EnemyController : MonoBehaviour, IReachPlayer, IDisappear
{
    [SerializeField] private EquationsCategoriesDatabase equationsCategoriesDatabase;
    [SerializeField] private EnemySpawner enemySpawner;
    
    public event Action<EquationType> OnDisappear;
    public event Action OnAttackFinished;

    private EnemyFactory _factory;
    
    private EquationType _equationType;
    private EquationData _equationData;
    private EnemyStats _stats;
    
    private EnemyVisualController _visualController;
    private EnemyMovementController _movementController;
    
    private void Awake()
    {
        _visualController = GetComponent<EnemyVisualController>();
        _movementController = GetComponent<EnemyMovementController>();
    }

    private void OnEnable()
    {
        if(_movementController != null)
            _movementController.OnFlip += _visualController.Flip;
    }

    private void OnDisable()
    {
        if (_movementController != null)
            _movementController.OnFlip -= _visualController.Flip;
    }
    
    public void Instantiate(EquationType equationType, EnemyStats stats, PlayerController playerController)
    {
        Debug.Log(equationType);
        _equationType = equationType;
        _movementController.Instantiate(stats, playerController.gameObject.transform.position);
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
        _movementController.ReachPlayer();
        FinishAttack();
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
        _equationData = GetRandomEquationData(_equationType);
        _movementController.ResetValues();
        _visualController.ResetValues(_equationData);
    }
    
    private EquationData GetRandomEquationData(EquationType equationType)
    {
        EquationCategoryData equationCategoryData = equationsCategoriesDatabase.GetEquationCategoryData(equationType);
        if (equationCategoryData == null) return null;
        List<EquationData> equationDatas = equationCategoryData.Equations;
        int index = UnityEngine.Random.Range(0, equationDatas.Count);
        return equationDatas[index];
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