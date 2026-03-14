using System;
using UnityEngine;

[Serializable]
public class DifficultyLevelStats 
{
    [field: SerializeField] public int PassThreshold { get; private set; }
    [field: SerializeField] public int MinAnswers { get; private set; }
    [field: SerializeField] public int MaxNumber { get; private set; }
}