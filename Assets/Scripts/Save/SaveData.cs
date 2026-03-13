using System.Collections.Generic;

public class SaveData
{
    public int Money;
    public List<int> EquationLevels;
    public List<int> EquationHighScores;
    public List<bool> SelectedEquations;
    
    public SaveData(int money, Dictionary<EquationType, int> equationLevels, Dictionary<EquationType, int> equationHighScores, List<EquationType> selectedEquations)
    {
        Money = money;
        EquationLevels = new List<int>(equationLevels.Values);
        EquationHighScores = new List<int>(equationHighScores.Values);
        SelectedEquations = new List<bool>();
        foreach (EquationType type in System.Enum.GetValues(typeof(EquationType)))
        {
            if(selectedEquations.Contains(type))
                SelectedEquations.Add(true);
            else
                SelectedEquations.Add(false);
        }
    }

    public SaveData(SaveData saveData)
    {
        Money = saveData.Money;
        EquationLevels = saveData.EquationLevels;
        EquationHighScores = saveData.EquationHighScores;
        SelectedEquations = saveData.SelectedEquations;
    }
}