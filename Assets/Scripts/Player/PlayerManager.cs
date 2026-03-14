using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private EquationsCategoriesDatabase equationsDatabase;
    [SerializeField] private PlayerController playerController;

    public event Action<Dictionary<EquationType, int>, Dictionary<EquationType, int>> OnEndGameScoresCalculated;
    
    public int Money { get; private set; }
    public Dictionary<EquationType, int> EquationLevels { get; private set; }
    public Dictionary<EquationType, int> EquationHighScores { get; private set; }
    public List<EquationType> SelectedEquations { get; private set; }

    private void Start()
    {
        Money = SaveManager.Instance.Money;
        EquationLevels = SaveManager.Instance.EquationLevels;
        EquationHighScores = SaveManager.Instance.EquationHighScores;
        SelectedEquations = SaveManager.Instance.SelectedEquations;
    }
    
    private void OnEnable()
    {
        if (playerController == null)
            return;

        playerController.OnDeathWithData += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (playerController == null)
            return;

        playerController.OnDeathWithData -= HandlePlayerDeath;
    }

    private void ToggleSelectEquation(EquationType equationType)
    {
        if(SelectedEquations.Contains(equationType))
        {
            SelectedEquations.Remove(equationType);
        }
        else
        {
            SelectedEquations.Add(equationType);
        }
        Save();
    }

    public void SetEquationHighScore(EquationType equationType, int score)
    {
        EquationHighScores[equationType] = score;

        EquationCategoryData equationCategoryData = equationsDatabase.GetEquationCategoryData(equationType);
        List<int> achievementThresholds = equationCategoryData.AchievmentThresholds;

        int level = 0;

        for (int i = 0; i < achievementThresholds.Count; i++)
        {
            if (score >= achievementThresholds[i])
                level++;
            else
                break;
        }

        EquationLevels[equationType] = level;
    }
    
    public void HandlePlayerDeath(Dictionary<EquationType, int> equationScores)
    {
        Dictionary<EquationType, int> previousScores = new Dictionary<EquationType, int>(EquationHighScores);
        
        int additionScore = equationScores.TryGetValue(EquationType.Addition, out int addScore) ? addScore : 0;
        int subtractionScore = equationScores.TryGetValue(EquationType.Subtraction, out int subScore) ? subScore : 0;
        int multiplicationScore = equationScores.TryGetValue(EquationType.Multiplication, out int mulScore) ? mulScore : 0;
        int divisionScore = equationScores.TryGetValue(EquationType.Division, out int divScore) ? divScore : 0;
        
        additionScore += previousScores[EquationType.Addition];
        subtractionScore += previousScores[EquationType.Subtraction];
        multiplicationScore += previousScores[EquationType.Multiplication];
        divisionScore += previousScores[EquationType.Division];
        
        Debug.Log($"Player Death Scores - Addition: {additionScore}, Subtraction: {subtractionScore}, Multiplication: {multiplicationScore}, Division: {divisionScore}");

        Dictionary<EquationType, int> newScores = new Dictionary<EquationType, int>()
        {
            { EquationType.Addition, additionScore },
            { EquationType.Subtraction, subtractionScore },
            { EquationType.Multiplication, multiplicationScore },
            { EquationType.Division, divisionScore },
        };
        
        EquationHighScores = new Dictionary<EquationType, int>(newScores);

        Save();

        foreach (var score in newScores)
        {
            Debug.Log($"Equation: {score.Key}, Previous High Score: {previousScores[score.Key]}, New High Score: {score.Value}");
        }
        // Send previousScores and newScores to the end game UI here
        OnEndGameScoresCalculated?.Invoke(previousScores, newScores);
    }

    private void Save()
    {
        SaveData saveData = new SaveData(Money, EquationLevels, EquationHighScores, SelectedEquations);
        SaveManager.Instance.Save(saveData);
    }
}