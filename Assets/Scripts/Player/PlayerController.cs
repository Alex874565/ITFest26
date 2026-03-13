using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerVisualController))]
public class PlayerController : MonoBehaviour
{
    public Action<int> OnScoreChange;
    
    private int _score = 0;
    private Dictionary<EquationType, int> _equationScores;
    
    public void AddScore(EquationType equationType)
    {
        _equationScores[equationType]++;
        _score++;
        OnScoreChange?.Invoke(_score);
    }
}