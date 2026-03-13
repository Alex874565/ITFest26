using System;
using UnityEngine;

[Serializable]
public class EquationData
{
    [SerializeField] public EquationType Type;
    [field: SerializeField] public string Equation  { get; private set; }
    [field: SerializeField] public int Answer { get; private set; }
    
    public EquationData(EquationData data)
    {
        Type = data.Type;
        Equation = data.Equation;
        Answer = data.Answer;
    }
}