using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int Money;
    public List<int> EquationLevels;
    public List<int> EquationHighScores;
    public List<bool> SelectedEquations;
    public List<List<int>> Unlocks;
    
    public SaveData(int money, Dictionary<EquationType, int> equationLevels, Dictionary<EquationType, int> equationHighScores, List<EquationType> selectedEquations, List<List<int>> unlocks)
    {
        Money = money;
        EquationLevels = new List<int>(equationLevels.Values);
        EquationHighScores = new List<int>(equationHighScores.Values);
        SelectedEquations = new List<bool>();
        Unlocks = unlocks;
        foreach (EquationType type in System.Enum.GetValues(typeof(EquationType)))
        {
            if(selectedEquations.Contains(type))
                SelectedEquations.Add(true);
            else
                SelectedEquations.Add(false);
        }
    }
    
    public SaveData()
    {
        Money = 0;
        EquationLevels = new List<int> { 0, 0, 0, 0 };
        EquationHighScores = new List<int> { 0, 0, 0, 0 };
        SelectedEquations = new List<bool> { false, false, false, false };
    }

    public SaveData(SaveData saveData)
    {
        Money = saveData.Money;
        EquationLevels = saveData.EquationLevels;
        EquationHighScores = saveData.EquationHighScores;
        SelectedEquations = saveData.SelectedEquations;
    }
}