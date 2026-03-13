using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyMovementController), typeof(EnemyVisualController))]
public class EnemyController : MonoBehaviour, IReachPlayer, IDisappear
{
    [SerializeField] private EquationsCategoriesDatabase equationsCategoriesDatabase;
    
    public event Action<EquationType> OnDisappear;

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
    
    public void Instantiate(EquationType equationType, EnemyStats stats, PlayerController playerController)
    {
        _equationData = GetRandomEquationData(equationType);
        _movementController.Instantiate(stats, playerController.gameObject.transform.position);
        ResetValues();
        OnDisappear += playerController.AddScore;
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
        OnDisappear?.Invoke(_equationData.Type);
        _factory.ReturnToPool(this);
    }

    public void ReachPlayer()
    {
        _movementController.ReachPlayer();
    }

    public void SetFactory(EnemyFactory factory)
    {
        _factory = factory;
    }
    
    public void ResetValues()
    {
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
}