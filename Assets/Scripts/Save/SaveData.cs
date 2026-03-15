using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int Money;
    public List<int> EquationLevels;
    public List<int> EquationHighScores;
    public List<bool> SelectedEquations;
    public List<IntList> Unlocks;
    
    public SaveData(int money, Dictionary<EquationType, int> equationLevels, Dictionary<EquationType, int> equationHighScores, List<EquationType> selectedEquations, Dictionary<UnlockableType, List<CosmeticState>> unlocks)
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

        Unlocks = new List<IntList>();
        foreach (UnlockableType type in Enum.GetValues(typeof(UnlockableType)))
        {
            List<int> states = new List<int>();
            foreach (CosmeticState state in unlocks[type])
            {
                states.Add((int)state);
            }
            Unlocks.Add(new IntList { Values = states });
        }
    }
    
    public SaveData()
    {
        Money = 0;
        EquationLevels = new List<int> { 0, 0, 0, 0 };
        EquationHighScores = new List<int> { 0, 0, 0, 0 };
        SelectedEquations = new List<bool> { false, false, false, false };
        Unlocks = new List<IntList>();
        foreach (UnlockableType type in Enum.GetValues(typeof(UnlockableType)))
        {
            List<int> states = new List<int>();
            foreach (CosmeticState state in Enum.GetValues(typeof(CosmeticState)))
            {
                states.Add(-1);
            }

            if (type == UnlockableType.Background)
                states[0] = (int)CosmeticState.Equipped;
            
            Unlocks.Add(new IntList { Values = states });
        }
    }

    public SaveData(SaveData saveData)
    {
        Money = saveData.Money;
        EquationLevels = saveData.EquationLevels;
        EquationHighScores = saveData.EquationHighScores;
        SelectedEquations = saveData.SelectedEquations;
        Unlocks = saveData.Unlocks;
    }
}