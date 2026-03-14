using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private EquationsCategoriesDatabase equationsDatabase;
    [SerializeField] private UnlockablesDatabase unlockablesDatabase;
    [SerializeField] private PlayerController playerController;

    public event Action OnDataLoaded;
    public event Action<Dictionary<EquationType, int>, Dictionary<EquationType, int>, int> OnEndGameValuesCalculated;
    
    public int Money { get; private set; }
    public Dictionary<EquationType, int> EquationLevels { get; private set; }
    public Dictionary<EquationType, int> EquationHighScores { get; private set; }
    public Dictionary<UnlockableType, List<CosmeticState>> Unlocks { get; private set; }
    public List<EquationType> SelectedEquations { get; private set; }

    private void Start()
    {
        Money = SaveManager.Instance.Money;
        EquationLevels = SaveManager.Instance.EquationLevels;
        EquationHighScores = SaveManager.Instance.EquationHighScores;
        SelectedEquations = SaveManager.Instance.SelectedEquations;
        Unlocks = SaveManager.Instance.Unlocks;
        OnDataLoaded?.Invoke();
    }
    
    private void OnEnable()
    {
        if (playerController == null)
            return;

        playerController.OnDeathWithData += HandlePlayerDeath;
        playerController.OnDeath += () => SetMoney(Money + playerController.Money);
    }

    private void OnDisable()
    {
        if (playerController == null)
            return;

        playerController.OnDeathWithData -= HandlePlayerDeath;
    }

    #region Unlockables

    public bool CanAffordItem(UnlockableType unlockable, int index)
    {
        return Money >= unlockablesDatabase.GetUnlockable(unlockable, index).Cost;
    }

    public bool IsItemUnlocked(UnlockableType unlockable, int index)
    {
        return Unlocks[unlockable][index] == 0;
    }

    public bool IsItemEquipped(UnlockableType unlockable, int index)
    {
        return Unlocks[unlockable][index] > 0;
    }

    public UnlockableData GetEquippedItemData(UnlockableType unlockable)
    {
        if(Unlocks == null || Unlocks[unlockable] == null)
            return null;
        int index = Unlocks[unlockable].FindIndex(status => status == 1);
        return unlockablesDatabase.GetUnlockable(unlockable, index);
    }

    public void UnlockItem(UnlockableType unlockable, int index)
    {
        Unlocks[unlockable][index] = 0;
    }
    
    private void EquipUnlockable(UnlockableType unlockable, int index)
    {
        List<int> unlockablesStatus = Unlocks[unlockable];
        if (unlockablesStatus == null) return;
        
        for(int i = 0; i < unlockablesStatus.Count; i++)
        {
            if(unlockablesStatus[i] == 1) // If the item is unlocked
            {
                Unlocks[unlockable][i] = 0;
            }else if (i == index)
            {
                Unlocks[unlockable][i] = 1;
            }
        }
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
    
    #endregion

    #region Stats
    
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

    public void SetMoney(int money)
    {
        Debug.Log($"Money: {money}");
        Money = money;
        Save();
    }
    
    #endregion
    
    public void HandlePlayerDeath(Dictionary<EquationType, int> equationScores, int moneyGained)
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
        
        SetEquationHighScore(EquationType.Addition, additionScore);
        SetEquationHighScore(EquationType.Subtraction, subtractionScore);
        SetEquationHighScore(EquationType.Multiplication, multiplicationScore);
        SetEquationHighScore(EquationType.Division, divisionScore);
        
        EquationHighScores = new Dictionary<EquationType, int>(EquationHighScores);

        SetMoney(Money + moneyGained);

        foreach (var score in EquationHighScores)
        {
            Debug.Log($"Equation: {score.Key}, Previous High Score: {previousScores[score.Key]}, New High Score: {score.Value}");
        }
        // Send previousScores and newScores to the end game UI here
        OnEndGameValuesCalculated?.Invoke(previousScores, EquationHighScores, moneyGained);
    }

    private void Save()
    {
        SaveData saveData = new SaveData(Money, EquationLevels, EquationHighScores, SelectedEquations, Unlocks);
        SaveManager.Instance.Save(saveData);
    }
}