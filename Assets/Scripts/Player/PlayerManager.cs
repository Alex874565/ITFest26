using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private EquationsCategoriesDatabase equationsDatabase;
    
    public int Money { get; private set; }
    public Dictionary<EquationType, int> EquationLevels { get; private set; }
    public Dictionary<EquationType, int> EquationHighScores { get; private set; }
    public List<EquationType> SelectedEquations { get; private set; }

    private void Awake()
    {
        SaveManager.Instance.Load();
        if(SaveManager.Instance.SaveData != null)
        {
            Money = SaveManager.Instance.Money;
            EquationLevels = SaveManager.Instance.EquationLevels;
            EquationHighScores = SaveManager.Instance.EquationHighScores;
            SelectedEquations = SaveManager.Instance.SelectedEquations;
        }
        else
        {
            int money = 0;

            EquationLevels = new Dictionary<EquationType, int>
            {
                { EquationType.Addition, 0 },
                { EquationType.Subtraction, 0 },
                { EquationType.Multiplication, 0 },
                { EquationType.Division, 0 }
            };

            EquationHighScores = new Dictionary<EquationType, int>
            {
                { EquationType.Addition, 0 },
                { EquationType.Subtraction, 0 },
                { EquationType.Multiplication, 0 },
                { EquationType.Division, 0 }
            };

            SelectedEquations = new List<EquationType>();
        }
        Save();
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

    private void SetEquationHighScore(EquationType equationType, int score)
    {
        EquationHighScores[equationType] = score;
        EquationCategoryData equationCategoryData = equationsDatabase.GetEquationCategoryData(equationType);
        List<int> achievmentThresholds = equationCategoryData.AchievmentThresholds;
        for (int i = 0; i < achievmentThresholds.Count; i++)
        {
            if (achievmentThresholds[i] < score)
            {
                EquationLevels[equationType] = i - 1;
                break;
            }
        }
        Save();
    }

    private void Save()
    {
        SaveData saveData = new SaveData(Money, EquationLevels, EquationHighScores, SelectedEquations);
        SaveManager.Instance.Save(saveData);
    }
}